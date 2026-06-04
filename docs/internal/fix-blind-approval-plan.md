# Kế hoạch sửa "Duyệt mù" + Reject reason + Audit trail + Preview

## Tổng quan

4 vấn đề cần giải quyết:
1. **Duyệt mù** — người duyệt thiếu thông tin để ra quyết định (8 điểm).
2. **Reject không có lý do** — admin reject event nhưng organizer không biết tại sao.
3. **Không có history** — organizer không biết ai duyệt, khi nào.
4. **Không có preview** — admin không biết event hiển thị thế nào cho volunteer.

**Thời gian ước tính**: 6-8 giờ tổng.

---

## Phần A: Backend — Reject reason cho Event

### A1. Thêm field `RejectReason` vào Event entity

**File**: `BaseCore.Entities/Event.cs`

Thêm sau `CancelReason`:
```csharp
public string RejectReason { get; set; } = "";
```

### A2. Cấu hình DbContext

**File**: `BaseCore.Repository/MySqlDbContext.cs`

Trong `modelBuilder.Entity<Entities.Event>`, thêm:
```csharp
entity.Property(e => e.RejectReason).HasMaxLength(1000).IsRequired(false);
```

### A3. Sửa `EventService.RejectAsync` nhận reason

**File**: `BaseCore.Services/VolunteerHub/Events/EventService.cs`

Sửa signature:
```csharp
public async Task<Entities.Event> RejectAsync(int eventId, string? reason)
```

Trong body thêm:
```csharp
ev.Status = "Rejected";
ev.RejectReason = reason?.Trim() ?? "";
```

### A4. Sửa `IEventService`

```csharp
Task<Entities.Event> RejectAsync(int eventId, string? reason);
```

### A5. Sửa `EventsController.Reject` nhận body

**File**: `BaseCore.APIService/Controllers/Events/EventsController.cs`

```csharp
[HttpPut("{id}/reject"), Authorize(Roles = "Admin")]
[EnableRateLimiting("write-sensitive")]
public async Task<IActionResult> Reject(int id, [FromBody] EventRejectDto? dto)
{
    if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        return Unauthorized();
    try
    {
        var ev = await _eventService.RejectAsync(id, dto?.Reason);
        await RecordAuditAsync(userId, "Event.Reject", "Event", ev.Id, $"Reason={dto?.Reason}");
        return Ok(ev);
    }
    catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
}
```

Thêm DTO:
```csharp
public class EventRejectDto
{
    public string? Reason { get; set; }
}
```

### A6. Sửa notification khi reject

Trong `EventService.RejectAsync`, sửa notification message:
```csharp
var message = string.IsNullOrWhiteSpace(reason)
    ? $"Sự kiện '{ev.Title}' đã bị từ chối."
    : $"Sự kiện '{ev.Title}' đã bị từ chối. Lý do: {reason}";
await _notificationService.SendAsync(ev.OrganizerId, "Sự kiện bị từ chối", message, "EventRejected", eventId);
```

### A7. Migration

```powershell
dotnet ef migrations add AddEventRejectReason --project BaseCore.Repository --startup-project BaseCore.APIService --context MySqlDbContext
```

### A8. Build

```powershell
dotnet build BaseCore.sln
```

**Tiêu chí**: Build pass. Reject endpoint nhận `{ "reason": "..." }` và lưu vào DB + gửi notification kèm lý do.

---

## Phần B: Backend — Audit trail endpoint nhẹ

### B1. Thêm endpoint lấy audit log theo entity

**File**: `BaseCore.APIService/Controllers/Admin/AdminController.cs` (hoặc tạo endpoint mới trong EventsController)

Thêm endpoint public cho organizer xem history event của mình:

```csharp
[HttpGet("{id}/history"), Authorize]
public async Task<IActionResult> GetEventHistory(int id)
{
    if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        return Unauthorized();
    var role = User.FindFirst(ClaimTypes.Role)?.Value;

    var ev = await _eventService.GetByIdAsync(id);
    if (ev == null) return NotFound(new { message = "Event not found" });
    if (role != "Admin" && ev.OrganizerId != userId) return Forbid();

    var logs = await _context.AuditLogs
        .Where(a => a.EntityType == "Event" && a.EntityId == id)
        .OrderByDescending(a => a.CreatedAtUtc)
        .Take(50)
        .Select(a => new
        {
            a.Id,
            a.Action,
            a.Metadata,
            a.CreatedAtUtc,
            actorId = a.UserId,
            actorName = a.User != null ? a.User.Name : null
        })
        .ToListAsync();

    return Ok(logs);
}
```

**Vị trí**: Đặt trong `EventsController.cs` (route `GET /api/events/{id}/history`).

**Quyền**: Organizer sở hữu event hoặc Admin.

### B2. Frontend gọi

Trong `api.js`:
```javascript
getEventHistory: (eventId) => api.get(`/events/${eventId}/history`),
```

**Tiêu chí**: Organizer vào quản lý event → thấy được ai duyệt, khi nào, reject reason nếu có.

---

## Phần C: Frontend — Sửa "duyệt mù" (8 điểm)

### C1. AdminEvents — Thêm context khi duyệt event

**File**: `BaseCore.WebClient/src/pages/admin/AdminEvents.jsx`

**Hiện tại**: Mỗi event row chỉ có tên + status + nút Approve/Reject.

**Sửa thành**:

Mỗi event row thêm:
- Cột **Organizer** (tên + badge verified ✓)
- Cột **Thời gian** (startDate - endDate)
- Cột **Địa điểm** (location, rút gọn)
- Nút **"Xem"** → mở `/events/{id}` trong tab mới (`target="_blank"`)
- Khi bấm **Reject** → hiện modal nhập lý do (bắt buộc ít nhất 10 ký tự)

```jsx
// Nút xem chi tiết
<a href={`/events/${event.id}`} target="_blank" rel="noopener"
   className="btn-secondary btn-sm" title="Xem chi tiết sự kiện">
  <i className="fa-solid fa-eye" />
</a>

// Modal reject
<dialog>
  <h3>Từ chối sự kiện</h3>
  <p>Sự kiện: {event.title}</p>
  <textarea placeholder="Lý do từ chối (bắt buộc)..." minLength={10} required />
  <button onClick={() => rejectEvent(event.id, reason)}>Xác nhận từ chối</button>
</dialog>
```

**Tiêu chí**:
- ☐ Admin thấy organizer name + thời gian + địa điểm ngay trong danh sách
- ☐ Nút "Xem" mở event detail trong tab mới
- ☐ Reject bắt buộc nhập lý do ≥ 10 ký tự
- ☐ Organizer nhận notification có kèm lý do

---

### C2. AdminOrganizerVerifications — Thêm xem giấy tờ

**File**: `BaseCore.WebClient/src/pages/admin/AdminOrganizerVerifications.jsx`

**Sửa**:
- Hiển thị ảnh `documentUrl` dạng lightbox (click phóng to)
- Thêm link "Xem profile organizer" → `/profile/{organizerId}`
- Hiển thị số event organizer đã tạo (nếu có)
- Reject bắt buộc nhập lý do

**Tiêu chí**:
- ☐ Admin click ảnh giấy tờ → phóng to xem được
- ☐ Có link sang profile organizer
- ☐ Reject có input lý do

---

### C3. AdminVolunteerVerifications (KYC) — Thêm xem ảnh + profile

**File**: `BaseCore.WebClient/src/pages/admin/AdminVolunteerVerifications.jsx`

**Sửa**:
- 3 ảnh (CCCD trước, sau, chân dung) hiển thị dạng thumbnail, click phóng to
- Thêm link "Xem hồ sơ" → `/profile/{userId}`
- Hiển thị tên + email volunteer rõ hơn

**Tiêu chí**:
- ☐ Admin xem được 3 ảnh KYC rõ ràng
- ☐ Có link sang profile volunteer

---

### C4. AdminSkillVerifications — Thêm xem minh chứng

**File**: (trong AdminController hoặc trang riêng)

**Sửa**:
- Link `evidenceUrl` mở được (nếu là ảnh → hiển thị, nếu là link → mở tab mới)
- Hiển thị rõ: tên volunteer + tên skill + level

**Tiêu chí**:
- ☐ Admin click minh chứng → xem được nội dung

---

### C5. ManageEvent — Danh sách đăng ký thêm context volunteer

**File**: `BaseCore.WebClient/src/pages/organizer/ManageEvent.jsx`

**Hiện tại**: Mỗi registration row có tên + status + nút confirm/cancel.

**Sửa thành**:

Mỗi row thêm:
- Link **"Xem hồ sơ"** → `/profile/{userId}` tab mới
- Badge **KYC** (✓ Verified / ⚠ Chưa xác minh)
- Hiển thị **kỹ năng** volunteer (nếu có, dạng tag nhỏ)
- Hiển thị **ghi chú đăng ký** (`note`) nếu có
- Nếu `cancelRequested` → badge đỏ "Xin hủy" + hiển thị `cancelReason`

```jsx
<a href={`/profile/${reg.user?.id || reg.userId}`} target="_blank" className="text-primary-600 hover:underline text-xs">
  Xem hồ sơ
</a>
{reg.cancelRequested && (
  <span className="badge badge-red">Xin hủy: {reg.cancelReason}</span>
)}
```

**Tiêu chí**:
- ☐ Organizer thấy kỹ năng + KYC status của volunteer ngay trong danh sách
- ☐ Có link mở hồ sơ volunteer
- ☐ Cancel-request hiển thị lý do rõ ràng

---

### C6. ManageEvent — Cancel-request nổi bật

**Trong tab "Danh sách đăng ký"**:

Thêm filter/sort: đưa registration có `cancelRequested = true` lên đầu hoặc có section riêng "Yêu cầu hủy" với highlight vàng.

**Tiêu chí**:
- ☐ Organizer thấy ngay ai đang xin hủy mà không cần scroll tìm

---

### C7. MySponsorships (Sponsor) — Thêm context event khi duyệt lời mời

**File**: `BaseCore.WebClient/src/pages/sponsor/MySponsorships.jsx`

**Sửa**: Mỗi proposal (lời mời từ organizer) thêm:
- Link **"Xem sự kiện"** → `/events/{eventId}` tab mới
- Hiển thị: tên organizer + thời gian event + địa điểm + quy mô (maxParticipants)

**Tiêu chí**:
- ☐ Sponsor click "Xem sự kiện" → thấy đầy đủ thông tin event trước khi accept/reject

---

### C8. ManageEvent tab "Tài trợ doanh nghiệp" — Thêm context sponsor

**Trong tab `corporate`**:

Mỗi proposal từ sponsor thêm:
- Hiển thị: tên sponsor + email + phone (đã có trong response `sponsorName`, `sponsorEmail`)
- Link "Xem hồ sơ sponsor" (nếu có trang profile sponsor, hoặc hiển thị inline)

**Tiêu chí**:
- ☐ Organizer thấy thông tin sponsor trước khi accept/reject proposal

---

## Phần D: Frontend — Preview event trước khi approve

### D1. Nút "Preview" trong AdminEvents

Khi admin xem event Pending, thêm nút:

```jsx
<a href={`/events/${event.id}`} target="_blank" className="btn-secondary btn-sm" title="Xem như volunteer sẽ thấy">
  <i className="fa-solid fa-eye" /> Preview
</a>
```

Vấn đề: trang `/events/{id}` hiện chỉ hiển thị event `Approved`. Cần sửa để event `Pending` cũng hiển thị được nếu người xem là Admin.

### D2. Sửa EventDetail.jsx cho phép Admin xem event Pending

**File**: `BaseCore.WebClient/src/pages/public/EventDetail.jsx`

Hiện tại có thể đã chặn hiển thị event không phải Approved. Sửa:

```jsx
// Nếu event không Approved và user không phải Admin/Organizer sở hữu → ẩn
if (event.status !== 'Approved' && event.status !== 'Completed') {
  if (user?.role !== 'Admin' && user?.id !== event.organizerId) {
    return <p>Sự kiện chưa được duyệt.</p>;
  }
  // Admin/Organizer vẫn xem được + hiện banner cảnh báo
}
```

Thêm banner khi xem event chưa duyệt:

```jsx
{event.status === 'Pending' && (
  <div className="bg-yellow-50 border border-yellow-200 rounded p-3 mb-4 text-sm text-yellow-800">
    <i className="fa-solid fa-clock mr-2" />
    Sự kiện đang chờ duyệt. Đây là bản xem trước.
  </div>
)}
{event.status === 'Rejected' && (
  <div className="bg-red-50 border border-red-200 rounded p-3 mb-4 text-sm text-red-800">
    <i className="fa-solid fa-xmark mr-2" />
    Sự kiện đã bị từ chối. {event.rejectReason && `Lý do: ${event.rejectReason}`}
  </div>
)}
```

### D3. Backend: cho phép GET /api/events/{id} trả event Pending cho Admin

**File**: `BaseCore.APIService/Controllers/Events/EventsController.cs` → `GetById`

Hiện tại `GetById` trả event bất kể status (không filter). Vậy backend đã OK — chỉ cần frontend hiển thị đúng.

Kiểm tra: nếu `EventService.GetByIdAsync` có filter status → bỏ filter đó.

**Tiêu chí**:
- ☐ Admin bấm "Preview" → mở event detail trong tab mới
- ☐ Event Pending hiển thị đầy đủ + banner "Đang chờ duyệt"
- ☐ Event Rejected hiển thị + banner "Đã bị từ chối" + lý do
- ☐ Guest/Volunteer không xem được event Pending (vẫn bị chặn)

---

## Phần E: Frontend — Audit trail trong ManageEvent

### E1. Thêm tab "Lịch sử" hoặc section trong tab "Báo cáo"

**File**: `BaseCore.WebClient/src/pages/organizer/ManageEvent.jsx`

Thêm vào tab "Báo cáo" (hoặc tạo tab riêng "Lịch sử"):

```jsx
// Gọi API
useEffect(() => {
  eventApi.getEventHistory(id).then(r => setHistory(r.data || [])).catch(() => {});
}, [id]);

// Render
<div className="card p-4">
  <h3 className="font-semibold mb-3">Lịch sử thao tác</h3>
  <div className="space-y-2">
    {history.map(h => (
      <div key={h.id} className="flex items-center gap-3 text-sm border-l-2 border-gray-200 pl-3">
        <span className="text-gray-400 text-xs w-32">{formatDate(h.createdAtUtc)}</span>
        <span className="font-medium">{h.actorName || 'Hệ thống'}</span>
        <span className="text-gray-600">{translateAction(h.action)}</span>
      </div>
    ))}
  </div>
</div>
```

Helper translate:
```javascript
function translateAction(action) {
  const map = {
    'Event.Create': 'tạo sự kiện',
    'Event.Approve': 'duyệt sự kiện',
    'Event.Reject': 'từ chối sự kiện',
    'Event.Complete': 'hoàn thành sự kiện',
    'Event.Cancel': 'hủy sự kiện',
    'Event.Resubmit': 'gửi duyệt lại',
    'Event.Update': 'cập nhật sự kiện',
    'Event.Uncomplete': 'mở lại sự kiện',
    'Event.Transfer': 'chuyển quyền sở hữu',
    'Registration.Register': 'có volunteer đăng ký',
    'Registration.Confirm': 'xác nhận volunteer',
    'Registration.Cancel': 'hủy đăng ký volunteer',
    'Registration.CheckIn': 'điểm danh',
    'Registration.WalkIn': 'đăng ký tại chỗ',
  };
  return map[action] || action;
}
```

**Tiêu chí**:
- ☐ Organizer thấy timeline: ai tạo, ai duyệt, khi nào, ai reject (kèm lý do trong metadata)
- ☐ Hiển thị theo thứ tự mới nhất trước
- ☐ Tối đa 50 dòng

---

## Phần F: Cập nhật tài liệu

### F1. Sửa `docs/2-yeu-cau-chuc-nang.md`

Trong FR-08 (Duyệt và từ chối sự kiện), thêm:
```
- Khi từ chối, admin phải nhập lý do. Lý do được gửi kèm notification cho organizer.
```

Trong FR-07 (Tạo và sửa sự kiện), thêm:
```
- Organizer xem được lịch sử thao tác (ai duyệt, khi nào, lý do reject nếu có).
```

### F2. Sửa `docs/3-thiet-ke-he-thong.md`

Trong entity Event, thêm:
```
| RejectReason | string(1000) | Lý do admin từ chối |
```

Trong API Event Service, thêm:
```
GET    /api/events/{id}/history             [Owner/Admin] Lịch sử thao tác
```

---

## Test

### Test 1: Build

```powershell
dotnet build BaseCore.sln
cd BaseCore.WebClient && npm run build
```

**Tiêu chí**: Cả 2 pass.

---

### Test 2: Reject event với lý do

```powershell
# Login admin
$admT = (Login 'admin' 'admin123')

# Tạo event bằng organizer rồi reject với lý do
$orgT = (Login 'organizer' 'organizer123')
$ev = (Tạo event...)
Invoke-RestMethod -Method Put -Uri "http://localhost:5000/api/events/$($ev.id)/reject" -Headers (Hdr $admT) -Body '{"reason":"Thiếu mô tả chi tiết và ảnh minh họa"}' -ContentType 'application/json'
```

**Tiêu chí**:
- ☐ Event status = `Rejected`
- ☐ `rejectReason` = "Thiếu mô tả chi tiết và ảnh minh họa"
- ☐ Organizer nhận notification có kèm lý do

---

### Test 3: Event history

```powershell
$history = Invoke-RestMethod -Uri "http://localhost:5000/api/events/$($ev.id)/history" -Headers (Hdr $orgT)
# Kỳ vọng: có ít nhất 2 entry (Create + Reject), mỗi entry có actorName + action + createdAtUtc
```

**Tiêu chí**:
- ☐ Trả array có ≥ 2 items
- ☐ Mỗi item có `action`, `actorName`, `createdAtUtc`

---

### Test 4: Preview event Pending

1. Mở browser, login admin
2. Vào `/admin/events`, tìm event Pending
3. Bấm nút "Xem" / "Preview"
4. **Kỳ vọng**: Event detail hiển thị đầy đủ + banner vàng "Đang chờ duyệt"

---

### Test 5: Organizer thấy lý do reject

1. Login organizer
2. Vào `/my-events`
3. Event bị Rejected hiển thị lý do (hoặc vào event detail thấy banner đỏ + lý do)

---

### Test 6: ManageEvent danh sách đăng ký có context

1. Login organizer, vào quản lý event có volunteer đăng ký
2. **Kỳ vọng**: Mỗi volunteer row có link "Xem hồ sơ", badge KYC, ghi chú đăng ký

---

### Test 7: Sponsor thấy context event khi nhận lời mời

1. Login sponsor, vào `/my-sponsorships`
2. Nếu có proposal từ organizer → **Kỳ vọng**: có link "Xem sự kiện", thấy tên organizer + thời gian

---

## Checklist tổng

| # | Việc | Tiêu chí | ☐ |
|---|---|---|---|
| A | Event.RejectReason field + migration | DB có cột mới | ☐ |
| A | Reject endpoint nhận reason | API trả rejectReason | ☐ |
| A | Notification kèm lý do | Organizer nhận message có reason | ☐ |
| B | GET /api/events/{id}/history | Trả audit log theo event | ☐ |
| C1 | AdminEvents: cột organizer + thời gian + nút Xem + modal reject | Admin thấy context | ☐ |
| C2 | AdminOrganizerVerifications: lightbox giấy tờ | Admin xem ảnh lớn | ☐ |
| C3 | AdminVolunteerVerifications: lightbox 3 ảnh + link profile | Admin xem KYC rõ | ☐ |
| C4 | AdminSkillVerifications: link minh chứng | Admin mở được evidence | ☐ |
| C5 | ManageEvent registrations: link hồ sơ + KYC badge + skills | Organizer thấy context volunteer | ☐ |
| C6 | ManageEvent cancel-request nổi bật | Xin hủy hiện đầu + lý do | ☐ |
| C7 | MySponsorships: link xem event + organizer info | Sponsor thấy context | ☐ |
| C8 | ManageEvent proposals: sponsor info | Organizer thấy context sponsor | ☐ |
| D | Preview event Pending cho Admin | Banner + hiển thị đầy đủ | ☐ |
| D | Guest/Volunteer không xem Pending | Vẫn bị chặn | ☐ |
| E | Audit trail trong ManageEvent | Timeline hiển thị đúng | ☐ |
| F | Docs cập nhật | FR-08 + entity + API | ☐ |

---

## Thứ tự triển khai khuyến nghị

1. **A** (backend reject reason) — 30 phút, ảnh hưởng ít
2. **B** (history endpoint) — 30 phút, endpoint mới
3. **D** (preview) — 30 phút, sửa frontend nhẹ
4. **C1** (AdminEvents) — 1 giờ, quan trọng nhất
5. **C5 + C6** (ManageEvent registrations) — 1 giờ
6. **C7 + C8** (Sponsor/Proposal context) — 1 giờ
7. **C2 + C3 + C4** (Admin verifications) — 1 giờ
8. **E** (audit trail UI) — 1 giờ
9. **F** (docs) — 15 phút

Mỗi bước build + test ngay. Không cần làm hết 1 lần — có thể chia 2-3 session.
