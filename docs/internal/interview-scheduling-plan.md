# Kế hoạch triển khai — Phỏng vấn trực tuyến (đặt lịch + link họp ngoài)

> **Mục tiêu:** Bổ sung phân hệ phỏng vấn xen giữa `Pending → Confirmed` của đăng ký, dùng link họp ngoài (Meet/Zoom/Teams). Không tự xây video.
> **Nguyên tắc:** Bám đúng convention hiện tại. Phỏng vấn là **tùy chọn** — organizer vẫn confirm thẳng được.

---

## 0. Convention chốt (từ recon)

- Entities **phẳng** trong `BaseCore.Entities/` (không subfolder).
- DbContext: `BaseCore.Repository/MySqlDbContext.cs` — **provider SQL Server** (tên class gây hiểu nhầm). Migration auto-run lúc startup qua `DatabaseMigrationRunner`.
- `Status` lưu **string**, không enum.
- Controller: route **tuyệt đối từng method**, `[Authorize(Roles=...)]`, userId từ `User.FindFirst(ClaimTypes.NameIdentifier)`, mutation bọc try/catch `BadRequest(new { message })` + `_auditLogService.RecordAsync(...)`.
- **Confirm/Cancel là `PUT`** (không phải POST).
- Notification: `INotificationService.SendAsync(int userId, string title, string message, string type, int? relatedId = null)`.
- DTO request: **plain class inline** ở cuối file controller (không dùng record).
- DI service phải đăng ký ở **tất cả host** đang khai báo (APIService + AuthService + EventService + FinanceService) — mirror theo `IRegistrationService`.
- Background job = **queue table + poller** (không Hangfire/hosted service). → Nhắc lịch để **Phase 2 tùy chọn**.

---

## 1. Model dữ liệu

### 1.1 Entity mới — `BaseCore.Entities/InterviewSlot.cs`
```csharp
public class InterviewSlot
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }     // FK, unique (1 đăng ký = 1 slot)
    public int EventId { get; set; }             // denormalize để query theo sự kiện
    public DateTime ScheduledAt { get; set; }    // UTC
    public int DurationMinutes { get; set; } = 30;
    public string MeetingUrl { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;        // hướng dẫn cho TNV
    public string Status { get; set; } = "Scheduled";        // Scheduled | Passed | Failed | NoShow | Cancelled
    public string DecisionNote { get; set; } = string.Empty; // lý do đạt/không đạt
    public int CreatedBy { get; set; }           // organizerId
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Registration? Registration { get; set; }
    public Event? Event { get; set; }
}
```

### 1.2 Sửa `BaseCore.Entities/Registration.cs`
Thêm cột denormalize để lọc/hiển thị nhanh trong RegistrationsTab (khỏi join):
```csharp
public string? InterviewStatus { get; set; }   // null | Scheduled | Passed | Failed | NoShow | Cancelled
public InterviewSlot? InterviewSlot { get; set; }  // nav 1:1
```

### 1.3 (Tùy chọn, Phase 2) `BaseCore.Entities/Event.cs`
```csharp
public bool RequiresInterview { get; set; } = false;  // bắt buộc PV trước khi confirm
```

### 1.4 `MySqlDbContext.cs`
```csharp
public DbSet<InterviewSlot> InterviewSlots { get; set; }

// OnModelCreating
modelBuilder.Entity<InterviewSlot>(e =>
{
    e.HasIndex(x => x.RegistrationId).IsUnique();
    e.HasIndex(x => new { x.EventId, x.Status });
    e.Property(x => x.Status).HasMaxLength(20);
    e.Property(x => x.MeetingUrl).HasMaxLength(500);
    e.HasOne(x => x.Registration).WithOne(r => r.InterviewSlot)
        .HasForeignKey<InterviewSlot>(x => x.RegistrationId)
        .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId)
        .OnDelete(DeleteBehavior.NoAction); // tránh multiple cascade path
});
modelBuilder.Entity<Registration>().Property(r => r.InterviewStatus).HasMaxLength(20);
```

### 1.5 Migration
```
dotnet ef migrations add AddInterviewSlot -p BaseCore.Repository
```
(Tự apply lúc startup; không cần `database update` thủ công.)

---

## 2. Máy trạng thái

```
Registration.Status: Pending
  │
  ├─ Organizer confirm thẳng ─────────────► Confirmed   (giữ nguyên luồng cũ)
  │
  └─ Organizer "Hẹn PV"  → tạo InterviewSlot(Scheduled), Reg.InterviewStatus="Scheduled"
            │ notify TNV (kèm giờ + link)
            ├─ "Đổi lịch"  → cập nhật slot, notify
            ├─ "Hủy lịch"  → slot.Status="Cancelled", Reg.InterviewStatus="Cancelled" → vẫn Pending
            └─ Chấm kết quả:
                  ├─ Passed → slot.Status="Passed";  gọi ConfirmAsync → Reg.Confirmed
                  └─ Failed → slot.Status="Failed";  gọi CancelAsync  → Reg.Cancelled (+lý do)
```

Reg.Status chỉ chuyển Confirmed/Cancelled qua **logic confirm/cancel có sẵn** (đảm bảo capacity, CurrentParticipants…).

---

## 3. Service layer

### 3.1 `BaseCore.Services/VolunteerHub/Events/IInterviewService.cs`
```csharp
public interface IInterviewService
{
    Task<InterviewSlot> ScheduleAsync(int eventId, int regId, int organizerId, ScheduleInterviewInput input);
    Task<InterviewSlot> UpdateAsync(int eventId, int regId, int organizerId, ScheduleInterviewInput input);
    Task CancelAsync(int eventId, int regId, int organizerId);
    Task<InterviewSlot> DecideAsync(int eventId, int regId, int organizerId, string outcome, string note);
}
```
> `ScheduleInterviewInput` = POCO ở Services (hoặc nhận tham số rời). DTO controller map sang.

### 3.2 `InterviewService.cs`
- Ctor inject: `MySqlDbContext _context`, `INotificationService _notifications`, `IRegistrationService _registrations`.
- **Quy tắc (throw new Exception):**
  - Load reg `.Include(r => r.Event).Include(r => r.InterviewSlot)`; check `reg.EventId == eventId`, `reg.Event.OrganizerId == organizerId`.
  - Schedule: chỉ khi `reg.Status == "Pending"`; `ScheduledAt > DateTime.UtcNow` và `<= reg.Event.StartDate`; `MeetingUrl` bắt đầu `http://`/`https://`; nếu đã có slot active → dùng Update.
  - Decide: chỉ khi có slot `Scheduled`; `outcome ∈ {Passed, Failed, NoShow}`.
    - `Passed` → `await _registrations.ConfirmAsync(eventId, regId, organizerId)` (tái dùng kiểm tra capacity).
    - `Failed`/`NoShow` → `await _registrations.CancelAsync(eventId, regId, organizerId)` (+ lý do vào DecisionNote).
  - Cập nhật `reg.InterviewStatus` đồng bộ với `slot.Status`.
- **Notify** sau mỗi thao tác (mục 4).
- Đăng ký DI ở `Program.cs` của **APIService + AuthService + EventService + FinanceService**:
  `services.AddScoped<IInterviewService, InterviewService>();`

> Nếu muốn gọn hơn, có thể nhét các method này vào `RegistrationService` sẵn có thay vì tạo service mới — nhưng tách `InterviewService` rõ ràng hơn.

---

## 4. Thông báo (thêm hằng `type`)

| Hành động | `SendAsync(...)` | type |
|-----------|------------------|------|
| Hẹn PV | userId=reg.UserId, title="Lịch phỏng vấn", message="BTC mời bạn phỏng vấn lúc {local}. Link: {url}", relatedId=eventId | `InterviewScheduled` |
| Đổi lịch | … "Lịch phỏng vấn đã đổi sang {local}" | `InterviewUpdated` |
| Hủy lịch | … "Buổi phỏng vấn đã bị hủy" | `InterviewCancelled` |
| Đạt | … "Bạn đã được nhận vào sự kiện" | `InterviewPassed` |
| Không đạt | … "Rất tiếc, hồ sơ chưa phù hợp" (+lý do) | `InterviewFailed` |

> Hiển thị giờ local ở client; message lưu sẵn chuỗi giờ đã format hoặc để client format từ `relatedId`/payload. Đơn giản nhất: format giờ VN khi tạo message.

---

## 5. API — `BaseCore.APIService/Controllers/Events/RegistrationsController.cs`

```csharp
[HttpPost("api/events/{eventId}/registrations/{regId}/interview"), Authorize(Roles = "Organizer")]
[EnableRateLimiting("write-sensitive")]
public async Task<IActionResult> ScheduleInterview(int eventId, int regId, [FromBody] ScheduleInterviewDto dto) { ... }

[HttpPut("api/events/{eventId}/registrations/{regId}/interview"), Authorize(Roles = "Organizer")]
public async Task<IActionResult> UpdateInterview(int eventId, int regId, [FromBody] ScheduleInterviewDto dto) { ... }

[HttpPut("api/events/{eventId}/registrations/{regId}/interview/outcome"), Authorize(Roles = "Organizer")]
public async Task<IActionResult> DecideInterview(int eventId, int regId, [FromBody] InterviewOutcomeDto dto) { ... }

[HttpDelete("api/events/{eventId}/registrations/{regId}/interview"), Authorize(Roles = "Organizer")]
public async Task<IActionResult> CancelInterview(int eventId, int regId) { ... }
```
- userId từ claim; try/catch → `BadRequest(new { message = ex.Message })`; `_auditLogService.RecordAsync(userId, "Interview.Schedule"/"Interview.Decide"/..., "InterviewSlot", slot.Id, metadata, ip)`.
- Inject `IInterviewService` vào controller.

**DTO inline (cuối file):**
```csharp
public class ScheduleInterviewDto {
    public DateTime ScheduledAt { get; set; }
    public string MeetingUrl { get; set; } = "";
    public int? DurationMinutes { get; set; }
    public string Note { get; set; } = "";
}
public class InterviewOutcomeDto {
    public string Outcome { get; set; } = "";   // Passed | Failed | NoShow
    public string Note { get; set; } = "";
}
```

**Đọc dữ liệu phía TNV:** đảm bảo các endpoint trả đăng ký kèm slot:
- `GetRegistrations(eventId)` (organizer) và `getMyRegistrations` (TNV): thêm `.Include(r => r.InterviewSlot)` để FE có `registration.interviewSlot` + `interviewStatus`.

---

## 6. Frontend

### 6.1 `BaseCore.WebClient/src/services/api.js` — bổ sung vào `registrationApi`
```js
scheduleInterview: (eventId, regId, data) => api.post(`/events/${eventId}/registrations/${regId}/interview`, data),
updateInterview:   (eventId, regId, data) => api.put(`/events/${eventId}/registrations/${regId}/interview`, data),
decideInterview:   (eventId, regId, data) => api.put(`/events/${eventId}/registrations/${regId}/interview/outcome`, data),
cancelInterview:   (eventId, regId)        => api.delete(`/events/${eventId}/registrations/${regId}/interview`),
```
(Base URL đã có prefix `/api`.)

### 6.2 Tổ chức — `ManageEvent/index.jsx` + `RegistrationsTab.jsx`
- `index.jsx`: thêm state + handlers (`scheduleInterview`, `decideInterview`, `cancelInterview`), 1 modal "Hẹn phỏng vấn" (datetime-local, ô link họp, duration, note), 1 modal "Kết quả không đạt" (lý do). Reload registrations sau thao tác. Truyền props xuống `RegistrationsTab`.
- `RegistrationsTab.jsx`: với reg `Pending`:
  - Chưa có slot → nút **"Hẹn phỏng vấn"**.
  - Có slot `Scheduled` → badge "Đã hẹn {giờ} · [Mở phòng]({meetingUrl})", nút **"Đạt"**, **"Không đạt"**, "Đổi lịch", "Hủy lịch".
  - Slot `Passed/Failed` → badge trạng thái.
  - Dùng tone ink đã thống nhất.

### 6.3 TNV — `Activity.jsx` (và `MyRegistrations.jsx`)
- Card phỏng vấn lấy từ `registration.interviewSlot`: giờ hẹn (format VN), nút **"Vào phòng họp"** (`<a href={meetingUrl} target="_blank" rel="noreferrer">`), ghi chú BTC, trạng thái.
- *(Tùy chọn)* nút **"Thêm vào lịch"** → link Google Calendar template / file `.ics` (không cần API).

### 6.4 (Tùy chọn, Phase 2) `EventForm.jsx`
- Toggle "Yêu cầu phỏng vấn trước khi nhận" → set `RequiresInterview`. Khi bật, chặn confirm thẳng (enforce ở `RegistrationService.ConfirmAsync`).

---

## 7. Edge cases
- Hủy đăng ký / sự kiện bị hủy/hoàn thành → khóa thao tác PV; slot cascade-delete khi xóa reg.
- Timezone: lưu UTC, hiển thị local; `ScheduledAt` phải > now và ≤ `Event.StartDate`.
- `MeetingUrl` rỗng / không phải http(s) → chặn lưu (validate cả server lẫn client). *(Tùy chọn whitelist domain meet.google.com / zoom.us / teams.microsoft.com.)*
- Bảo mật: `meetingUrl` chỉ trả cho TNV liên quan + organizer sở hữu sự kiện; không lộ ở endpoint công khai.
- Đổi lịch nhiều lần: ghi đè slot + `UpdatedAt`; lịch sử ghi qua `_auditLogService`.
- NoShow: cho phép đánh dấu (không tự confirm).

---

## 8. Phân rã & thứ tự (MVP)

| GĐ | Việc | File chính | Mức |
|----|------|-----------|-----|
| 1 | Entity + DbContext + migration | `InterviewSlot.cs`, `Registration.cs`, `MySqlDbContext.cs` | Thấp |
| 2 | `IInterviewService` + `InterviewService` + DI 4 host | `Services/VolunteerHub/Events/`, 4× `Program.cs` | TB |
| 3 | Controller endpoints + DTO + Include slot vào payload | `RegistrationsController.cs` | TB |
| 4 | Notifications (5 type) | trong `InterviewService` | Thấp |
| 5 | FE tổ chức (modal + badge + nút) | `ManageEvent/index.jsx`, `RegistrationsTab.jsx`, `api.js` | TB |
| 6 | FE tình nguyện viên (card + join link) | `Activity.jsx`, `MyRegistrations.jsx` | Thấp |
| 7 | *(Tùy chọn)* Thêm vào lịch (.ics/Google) | FE | Thấp |
| 8 | *(Tùy chọn, Phase 2)* `RequiresInterview` + enforce | `Event.cs`, `EventForm.jsx`, `RegistrationService` | TB |
| 9 | *(Tùy chọn, Phase 2)* Nhắc lịch trước X giờ | queue table + poller (theo pattern CertificateJob) | Cao |

**MVP = GĐ 1→6.** Có ngay phỏng vấn trực tuyến thật, đúng yêu cầu mô tả.

---

## 9. Acceptance criteria (MVP)
- [ ] Organizer hẹn được lịch PV cho đăng ký `Pending` (giờ + link + note); TNV nhận notification.
- [ ] TNV thấy card PV và bấm "Vào phòng họp" mở đúng link.
- [ ] Organizer đổi/hủy lịch → TNV được thông báo.
- [ ] Chấm "Đạt" → đăng ký thành `Confirmed` (qua ConfirmAsync, đúng capacity); "Không đạt" → `Cancelled` + lý do; TNV được thông báo.
- [ ] Link họp không lộ ở endpoint công khai.
- [ ] `dotnet ef migrations` chạy sạch; build BE + FE sạch.

---

## 10. Trạng thái
- **2026-05-27** — Lập kế hoạch xong.
- **2026-05-27** — Triển khai xong. BE compile sạch (0 lỗi), FE build sạch, migration `AddInterviewSlot` đã áp dụng vào LocalDB.
- **GĐ1 — Entity + DbContext + migration:** ✅ `InterviewSlot.cs`; `Registration.InterviewStatus` + nav; `Event.RequiresInterview`; DbSet + config; migration `20260527063933_AddInterviewSlot` (đã sửa bỏ phần SponsorProfiles thừa do snapshot repo lệch).
- **GĐ2 — InterviewService + DI:** ✅ `IInterviewService`/`InterviewService` (Schedule/Update/Cancel/Decide), tái dùng `ConfirmAsync`/`CancelAsync`; DI ở 4 host.
- **GĐ3 — Controller + payload:** ✅ 4 endpoint (POST/PUT/PUT outcome/DELETE) + DTO; `Include(InterviewSlot)` vào GetByEvent/GetByUser.
- **GĐ4 — Notifications:** ✅ 5 type (Scheduled/Updated/Cancelled/Passed/Failed).
- **GĐ5 — FE tổ chức:** ✅ `registrationApi` 4 method; modal hẹn/đổi lịch + handlers trong `ManageEvent`; badge + nút (Đạt/Không đạt/Đổi/Hủy/Hẹn PV) trong `RegistrationsTab`.
- **GĐ6 — FE tình nguyện viên:** ✅ Card phỏng vấn + nút "Vào phòng họp" + "Thêm vào lịch" (Google Calendar) trong `Activity.jsx`.
- **GĐ7 — Thêm vào lịch:** ✅ link Google Calendar template (không cần API).
- **GĐ8 — RequiresInterview:** ✅ cờ ở `Event` + DTO create/update + enforce trong `ConfirmAsync` (bypass khi chấm Đạt) + toggle trong `EventForm`.
- **GĐ9 — Nhắc lịch nền:** ⬜ Chưa làm (cần hạ tầng job; ngoài MVP).
- **Hạ tầng:** thêm `DesignTimeDbContextFactory` cho `BaseCore.Repository` để tạo/áp dụng migration mà không cần host đang chạy.

> **Lưu ý vận hành:** 5 host backend đang chạy trong Visual Studio nạp code cũ. Schema đã được cập nhật; cần **rebuild/restart các host** để nạp code mới (endpoint + service) thì luồng phỏng vấn mới hoạt động end-to-end.
