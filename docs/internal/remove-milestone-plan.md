# Kế hoạch xóa Milestone + Nâng cấp UI tiến độ tài chính

## Tổng quan

Xóa toàn bộ `SponsorProjectMilestone` (entity, DB, API, UI) và thay bằng progress summary tính tự động từ dữ liệu campaign/proposal đã có.

**Thời gian ước tính**: 2-3 giờ.

---

## Phần 1: Xóa Milestone (Backend)

### Bước 1.1: Xóa entity

**File**: `BaseCore.Entities/SponsorProjectMilestone.cs`

**Hành động**: Xóa toàn bộ file.

---

### Bước 1.2: Xóa DbSet + config trong DbContext

**File**: `BaseCore.Repository/MySqlDbContext.cs`

**Xóa**:
- Dòng `public DbSet<SponsorProjectMilestone> SponsorProjectMilestones { get; set; }`
- Toàn bộ block `modelBuilder.Entity<SponsorProjectMilestone>(entity => { ... });`

---

### Bước 1.3: Xóa endpoint milestone trong SponsorController

**File**: `BaseCore.APIService/Controllers/Finance/SponsorController.cs`

**Xóa** các method:
- `GetMilestones`
- `CreateMilestone`
- `UpdateMilestone`
- `DeleteMilestone`
- `GetMySponsorshipTracking` (viết lại ở bước 3 — không dùng milestone nữa)

**Xóa** các helper:
- `GetMilestonesForEvent`
- `CanReadMilestonesAsync`
- `CanWriteMilestones`
- `ValidateMilestone`
- `NormalizeMilestoneStatus`
- `ApplyCompletionFields`
- `ClampProgress`
- `MapMilestoneStatusForTimeline`
- `CalculateFallbackProgress`
- `BuildFallbackTimeline`

**Xóa** DTO:
- `SponsorProjectMilestoneDto`

---

### Bước 1.4: Xóa route milestone trong ocelot.json

**File**: `BaseCore.ApiGateway/ocelot.json`

**Xóa** 2 route block:
```json
{
  "DownstreamPathTemplate": "/api/events/{eventId}/sponsor-milestones/{everything}",
  ...
},
{
  "DownstreamPathTemplate": "/api/events/{eventId}/sponsor-milestones",
  ...
}
```

---

### Bước 1.5: Tạo migration DROP TABLE

```powershell
cd D:\FW\FW\BaseCore
dotnet ef migrations add RemoveSponsorProjectMilestones --project BaseCore.Repository --startup-project BaseCore.APIService --context MySqlDbContext
```

**Kiểm tra** file migration vừa tạo:
- Phải có `migrationBuilder.DropTable(name: "SponsorProjectMilestones")`
- KHÔNG được drop bảng khác

---

### Bước 1.6: Build backend

```powershell
dotnet build BaseCore.sln
```

**Tiêu chí**: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Phần 2: Xóa Milestone (Frontend)

### Bước 2.1: Xóa API calls trong api.js

**File**: `BaseCore.WebClient/src/services/api.js`

**Xóa** trong `sponsorApi`:
```javascript
getMilestones: (eventId) => api.get(`/events/${eventId}/sponsor-milestones`),
createMilestone: (eventId, data) => api.post(`/events/${eventId}/sponsor-milestones`, data),
updateMilestone: (eventId, milestoneId, data) => api.put(`/events/${eventId}/sponsor-milestones/${milestoneId}`, data),
deleteMilestone: (eventId, milestoneId) => api.delete(`/events/${eventId}/sponsor-milestones/${milestoneId}`),
```

---

### Bước 2.2: Xóa milestone trong ManageEvent.jsx

**File**: `BaseCore.WebClient/src/pages/organizer/ManageEvent.jsx`

**Xóa**:
1. State declarations: `milestones`, `milestoneModal`, `editingMilestone`, `milestoneSaving`, `milestoneForm`
2. Trong `Promise.all`: xóa `sponsorApi.getMilestones(id)` và biến `milestoneRes` trong `.then()`
3. Xóa `setMilestones(milestoneRes.data || [])` trong `.then()`
4. Xóa toàn bộ functions: `resetMilestoneForm`, `openCreateMilestone`, `openEditMilestone`, `reloadMilestones`, `submitMilestone`, `deleteMilestone`
5. Xóa biến tính toán: `completedMilestones`, `projectProgress` (sẽ thay bằng logic mới ở Phần 3)
6. Xóa tab `{ key: 'milestones', label: 'Tiến độ tài trợ', icon: 'fa-timeline' }` trong danh sách tabs
7. Xóa toàn bộ block `{tab === 'milestones' && ( ... )}` (bao gồm cả modal form)

---

### Bước 2.3: Xóa milestone trong MySponsorships.jsx (nếu có)

**File**: `BaseCore.WebClient/src/pages/sponsor/MySponsorships.jsx`

Tìm và xóa phần hiển thị milestone/timeline trong tracking view. Thay bằng progress summary mới (Phần 3).

---

### Bước 2.4: Build frontend

```powershell
cd BaseCore.WebClient
npm run build
```

**Tiêu chí**: Build pass, không có import lỗi.

---

## Phần 3: Viết lại Sponsor Tracking (không dùng milestone)

### Bước 3.1: Viết lại `GetMySponsorshipTracking` trong SponsorController

**Logic mới**: Tính progress từ dữ liệu event + proposal + campaign, không cần milestone.

```csharp
[HttpGet("api/sponsors/my/{sponsorshipId}/tracking"), Authorize(Roles = "Sponsor")]
public async Task<IActionResult> GetMySponsorshipTracking(int sponsorshipId)
{
    // ... (giữ phần load sponsorship + event)
    
    // Progress tính từ event status
    var projectProgress = ev.Status switch
    {
        "Completed" => 100,
        "Cancelled" => 0,
        "Approved" when DateTime.UtcNow >= ev.StartDate => 75,
        "Approved" => 50,
        _ => 25
    };
    
    // Timeline tự sinh từ dữ liệu thật
    var timeline = new List<object>();
    timeline.Add(new { title = "Sự kiện được tạo", date = ev.CreatedAt, status = "Done" });
    timeline.Add(new { title = "Tài trợ ghi nhận", date = sponsorship.SponsoredAt, status = "Done" });
    if (ev.Status is "Approved" or "Completed")
        timeline.Add(new { title = "Sự kiện được duyệt", date = ev.CreatedAt, status = "Done" });
    timeline.Add(new { title = "Sự kiện diễn ra", date = ev.StartDate, 
        status = ev.Status == "Completed" || DateTime.UtcNow >= ev.StartDate ? "Done" : "Pending" });
    if (ev.Status == "Completed")
        timeline.Add(new { title = "Hoàn thành", date = ev.EndDate, status = "Done" });
    
    // ... return Ok(new { sponsorship, eventInfo, impact, timeline, projectProgress });
}
```

---

### Bước 3.2: Nâng cấp tab "Kêu gọi ủng hộ" trong ManageEvent.jsx

Thêm vào đầu tab `campaigns`:

```jsx
{/* Summary bar */}
<div className="card p-4 grid grid-cols-1 sm:grid-cols-4 gap-4">
  <div>
    <p className="text-xs text-gray-500">Tổng mục tiêu</p>
    <p className="text-xl font-bold">{formatMoney(campaigns.reduce((s, c) => s + c.targetAmount, 0))}</p>
  </div>
  <div>
    <p className="text-xs text-gray-500">Đã xác nhận</p>
    <p className="text-xl font-bold text-green-700">{formatMoney(campaigns.reduce((s, c) => s + (c.confirmedAmount || 0), 0))}</p>
  </div>
  <div>
    <p className="text-xs text-gray-500">Đang chờ</p>
    <p className="text-xl font-bold text-yellow-600">{formatMoney(campaigns.reduce((s, c) => s + (c.pendingAmount || 0), 0))}</p>
  </div>
  <div>
    <p className="text-xs text-gray-500">Tiến độ chung</p>
    <div className="mt-1">
      <div className="h-3 bg-gray-200 rounded-full overflow-hidden">
        <div className="h-full bg-green-500 rounded-full" style={{ width: `${overallProgress}%` }} />
      </div>
      <p className="text-xs text-gray-500 mt-1">{overallProgress}%</p>
    </div>
  </div>
</div>
```

Trong đó:
```jsx
const totalTarget = campaigns.reduce((s, c) => s + c.targetAmount, 0);
const totalConfirmed = campaigns.reduce((s, c) => s + (c.confirmedAmount || 0), 0);
const overallProgress = totalTarget > 0 ? Math.min(100, Math.round(totalConfirmed / totalTarget * 100)) : 0;
```

---

### Bước 3.3: Nâng cấp tab "Tài trợ doanh nghiệp" trong ManageEvent.jsx

Thêm vào đầu tab `corporate`:

```jsx
{/* Summary bar */}
<div className="card p-4 grid grid-cols-1 sm:grid-cols-4 gap-4">
  <div>
    <p className="text-xs text-gray-500">Đang chờ</p>
    <p className="text-xl font-bold text-yellow-600">{proposals.filter(p => p.status === 'Pending').length}</p>
  </div>
  <div>
    <p className="text-xs text-gray-500">Đã đồng ý</p>
    <p className="text-xl font-bold text-blue-600">{proposals.filter(p => p.status === 'Accepted').length}</p>
  </div>
  <div>
    <p className="text-xs text-gray-500">Đã nhận</p>
    <p className="text-xl font-bold text-green-700">{proposals.filter(p => p.status === 'Received' || p.status === 'Reported').length}</p>
  </div>
  <div>
    <p className="text-xs text-gray-500">Tổng đã nhận</p>
    <p className="text-xl font-bold text-green-700">
      {formatMoney(proposals.filter(p => p.status === 'Received' || p.status === 'Reported')
        .reduce((s, p) => s + (p.actualReceivedAmount || p.amount || 0), 0))}
    </p>
  </div>
</div>
```

---

### Bước 3.4: Nâng cấp tab "Báo cáo" trong ManageEvent.jsx

Thay `projectProgress` (cũ dùng milestone) bằng:

```jsx
const financialProgress = (() => {
  const totalTarget = campaigns.reduce((s, c) => s + c.targetAmount, 0);
  const totalConfirmed = campaigns.reduce((s, c) => s + (c.confirmedAmount || 0), 0);
  const proposalReceived = proposals.filter(p => p.status === 'Received' || p.status === 'Reported')
    .reduce((s, p) => s + (p.actualReceivedAmount || p.amount || 0), 0);
  const total = totalConfirmed + proposalReceived;
  const target = totalTarget + proposals.filter(p => p.status !== 'Rejected' && p.status !== 'Cancelled')
    .reduce((s, p) => s + (p.amount || 0), 0);
  return target > 0 ? Math.min(100, Math.round(total / target * 100)) : 0;
})();
```

---

## Phần 4: Cập nhật tài liệu

### Bước 4.1: Xóa FR-27 trong `docs/2-yeu-cau-chuc-nang.md`

Xóa toàn bộ block:
```markdown
### FR-27. Quản lý milestone tiến độ tài trợ
- Organizer tạo/sửa/xóa milestone cho event: ...
- Sponsor xem milestone của event mình tài trợ.
- Milestone tự đánh dấu Completed khi progress = 100%.
```

Đổi tiêu đề thành `## 1. Danh sách yêu cầu chức năng (27 FR)`.

Renumber FR-28 → FR-27.

---

### Bước 4.2: Xóa milestone trong `docs/3-thiet-ke-he-thong.md`

- Xóa dòng `- **SponsorProjectMilestone**: milestone tiến độ (...)` trong section entity.
- Xóa 4 endpoint milestone trong section Finance API.
- Sửa mô tả Finance service: bỏ "milestones" khỏi phạm vi.

---

### Bước 4.3: Xóa milestone trong `docs/5-phan-cong-nhom.md`

- Xóa dòng `- Milestone tiến độ tài trợ (CRUD)` trong phần C.
- Sửa `Phần campaign/sponsorship/milestone trong ManageEvent.jsx` → `Phần campaign/sponsorship trong ManageEvent.jsx`.

---

### Bước 4.4: Xóa milestone trong ocelot routing table (docs/3)

Xóa dòng:
```
| 175 | `/api/events/{id}/sponsor-milestones/*` | Finance 5004 |
```

---

### Bước 4.5: Sửa `scripts/reset-demo-data.sql`

Xóa 2 dòng:
```sql
DELETE FROM dbo.SponsorProjectMilestones;
DBCC CHECKIDENT ('dbo.SponsorProjectMilestones', RESEED, 0) WITH NO_INFOMSGS;
```

---

## Phần 5: Test

### Test 5.1: Build

```powershell
cd D:\FW\FW\BaseCore
dotnet build BaseCore.sln
cd BaseCore.WebClient
npm run build
```

**Tiêu chí**: Cả 2 pass, 0 error.

---

### Test 5.2: Start services + migration

```powershell
dotnet run --project BaseCore.EventService --urls http://localhost:5003
```

**Tiêu chí**: Service start thành công, migration `RemoveSponsorProjectMilestones` applied, bảng bị DROP.

Verify:
```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "VolunteerHub" -E -Q "SELECT OBJECT_ID('SponsorProjectMilestones')"
```

**Kỳ vọng**: trả `NULL` (bảng không tồn tại).

---

### Test 5.3: API milestone endpoint trả 404

```powershell
# Login organizer
$body = @{ userName = "organizer"; password = "organizer123" } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "http://localhost:5002/api/auth/login" -Body $body -ContentType "application/json"
$hdr = @{ Authorization = "Bearer $($login.accessToken)" }

# Gọi endpoint milestone cũ → phải 404 hoặc không tồn tại
try {
    Invoke-RestMethod -Uri "http://localhost:5000/api/events/2/sponsor-milestones" -Headers $hdr
    Write-Host "FAIL: endpoint still exists"
} catch {
    Write-Host "OK: milestone endpoint removed"
}
```

---

### Test 5.4: Sponsor tracking vẫn hoạt động (không dùng milestone)

```powershell
# Login sponsor
$body = @{ userName = "sponsor"; password = "sponsor123" } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "http://localhost:5002/api/auth/login" -Body $body -ContentType "application/json"
$hdr = @{ Authorization = "Bearer $($login.accessToken)" }

# Lấy danh sách sponsorship
$list = Invoke-RestMethod -Uri "http://localhost:5000/api/sponsors/my" -Headers $hdr
Write-Host "Sponsorships: $($list.Count)"

# Nếu có sponsorship, test tracking
if ($list.Count -gt 0) {
    $tracking = Invoke-RestMethod -Uri "http://localhost:5000/api/sponsors/my/$($list[0].id)/tracking" -Headers $hdr
    Write-Host "Tracking: progress=$($tracking.projectProgress) timeline=$($tracking.timeline.Count) items"
}
```

**Tiêu chí**: Tracking trả về `projectProgress` (số) + `timeline` (array) mà KHÔNG có milestone.

---

### Test 5.5: ManageEvent không có tab "Tiến độ tài trợ"

1. Mở `http://localhost:3000`
2. Login `organizer / organizer123`
3. Vào `/my-events` → mở quản lý event đã Approved
4. Kiểm tra danh sách tab:
   - ✅ Có: Đăng ký, Ca làm việc, Điểm danh, Kêu gọi ủng hộ, Tài trợ doanh nghiệp, Báo cáo
   - ❌ KHÔNG có: "Tiến độ tài trợ"

---

### Test 5.6: Tab "Kêu gọi ủng hộ" hiển thị progress

1. Trong ManageEvent, vào tab "Kêu gọi ủng hộ"
2. Kiểm tra:
   - Có summary bar: Tổng mục tiêu / Đã xác nhận / Đang chờ / Progress bar %
   - Nếu chưa có campaign → hiển thị 0/0/0/0%
   - Nếu có campaign với donation confirmed → progress bar > 0%

---

### Test 5.7: Tab "Tài trợ doanh nghiệp" hiển thị summary

1. Trong ManageEvent, vào tab "Tài trợ doanh nghiệp"
2. Kiểm tra:
   - Có summary bar: Đang chờ / Đã đồng ý / Đã nhận / Tổng đã nhận
   - Số liệu khớp với danh sách proposal bên dưới

---

## Checklist tổng

| # | Việc | Tiêu chí | ☐ |
|---|---|---|---|
| 1.1 | Xóa entity file | File không tồn tại | ☐ |
| 1.2 | Xóa DbSet + config | Không còn reference trong MySqlDbContext | ☐ |
| 1.3 | Xóa controller methods | SponsorController không còn milestone endpoint | ☐ |
| 1.4 | Xóa ocelot route | ocelot.json không còn sponsor-milestones | ☐ |
| 1.5 | Migration DROP TABLE | File migration có DropTable | ☐ |
| 1.6 | Backend build pass | 0 error | ☐ |
| 2.1 | Xóa api.js calls | Không còn getMilestones/create/update/delete | ☐ |
| 2.2 | Xóa ManageEvent milestone | Không còn tab + state + functions | ☐ |
| 2.3 | Xóa MySponsorships milestone | Tracking không dùng milestone | ☐ |
| 2.4 | Frontend build pass | 0 error | ☐ |
| 3.1 | Viết lại tracking | Trả progress + timeline từ event status | ☐ |
| 3.2 | UI campaign progress | Summary bar hiển thị đúng | ☐ |
| 3.3 | UI proposal summary | 4 ô đếm status hiển thị đúng | ☐ |
| 3.4 | UI report progress | Dùng financial progress thay milestone | ☐ |
| 4.x | Cập nhật docs | FR-27 xóa, entity xóa, API xóa, routing xóa | ☐ |
| 5.1 | Build pass | Backend + frontend | ☐ |
| 5.2 | Migration applied | Bảng bị DROP | ☐ |
| 5.3 | Endpoint 404 | Milestone route không tồn tại | ☐ |
| 5.4 | Tracking hoạt động | Trả progress + timeline | ☐ |
| 5.5 | UI không có tab milestone | Chỉ 6 tab | ☐ |
| 5.6 | Campaign progress bar | Hiển thị % đúng | ☐ |
| 5.7 | Proposal summary | 4 ô đếm đúng | ☐ |

---

## Lưu ý

- **Không xóa `EventSponsor`** — đây là bảng legacy tài trợ nhanh, vẫn dùng.
- **Không xóa `SponsorshipProposal`** — đây là bảng tài trợ chính thức, vẫn dùng.
- **Chỉ xóa `SponsorProjectMilestone`** — bảng cột mốc tiến độ.
- **Tracking endpoint giữ nguyên route** (`GET /api/sponsors/my/{id}/tracking`) — chỉ đổi logic bên trong.
- **Migration sẽ DROP TABLE** — dữ liệu milestone cũ mất. OK vì chỉ có demo data.
