# Thiết kế hệ thống — VolunteerHub

## 1. Kiến trúc tổng thể

```
┌─────────────────────────────────────────────────────────────┐
│                    React Frontend (3000)                      │
│                  BaseCore.WebClient (Vite)                    │
└──────────────────────────┬──────────────────────────────────┘
                           │ /api/*
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                  API Gateway (5000)                           │
│              BaseCore.ApiGateway (Ocelot)                     │
└───┬──────────────┬──────────────┬──────────────┬────────────┘
    │              │              │              │
    ▼              ▼              ▼              ▼
┌────────┐  ┌──────────┐  ┌───────────┐  ┌──────────┐
│Identity│  │  Event   │  │  Finance  │  │  Legacy  │
│ (5002) │  │  (5003)  │  │  (5004)   │  │  (5001)  │
└────────┘  └──────────┘  └───────────┘  └──────────┘
    │              │              │              │
    └──────────────┴──────────────┴──────────────┘
                           │
                           ▼
              ┌─────────────────────────┐
              │   SQL Server Database   │
              │      VolunteerHub       │
              └─────────────────────────┘
```

### Phân bổ service

| Service | Project | Port | Phạm vi |
|---|---|---:|---|
| Gateway | `BaseCore.ApiGateway` | 5000 | Routing, CORS, load balancing |
| Identity | `BaseCore.AuthService` | 5002 | Auth, JWT, profile, KYC, organizer verification, user management, notifications, badges, skills, monitoring |
| Event | `BaseCore.EventService` | 5003 | Event CRUD, registration, attendance, check-out, certificate, channel (SignalR), dashboard, ratings, event export |
| Finance | `BaseCore.FinanceService` | 5004 | Support campaign, donation, sponsorship proposal, sponsor profile, finance export |
| Legacy | `BaseCore.APIService` | 5001 | Fallback (Product, Order — domain cũ) |

### Shared layers (dùng chung giữa các service)

| Layer | Project | Vai trò |
|---|---|---|
| Entities | `BaseCore.Entities` | Domain model (POCO classes) |
| Repository | `BaseCore.Repository` | EF Core DbContext, migrations, repository pattern |
| Services | `BaseCore.Services` | Business logic |
| Common | `BaseCore.Common` | Utilities, helpers |

### Đặc điểm thiết kế

- **Shared database**: Tất cả service dùng chung 1 database `VolunteerHub` trên SQL Server. Phù hợp giai đoạn đồ án, tránh phức tạp đồng bộ dữ liệu.
- **Shared code via Compile Include**: EventService và FinanceService link controller từ APIService bằng `<Compile Include="...">` — không duplicate code.
- **JWT thống nhất**: Cùng secret key → token từ Identity validate được ở Event/Finance.
- **Migration tự động**: Mỗi service chạy `DatabaseMigrationRunner.RunWithProcessLock()` khi startup (dùng named Mutex để serialize).
- **IsActive middleware**: Tất cả 4 service đều có middleware kiểm tra `IsActive` — user bị khóa nhận 401 ngay lập tức.
- **SignalR hub**: EventService có SignalR hub (`/hubs/channel`) cho realtime channel (post/comment). Các service khác dùng `NullChannelRealtimeNotifier`.
- **Rate limiting**: API nhạy cảm (write-sensitive, read-sensitive) được rate limit.

## 2. Gateway Routing

Frontend gọi `/api/*` → Vite proxy về Gateway (5000) → Ocelot route theo priority:

| Priority | Pattern | Service |
|---:|---|---|
| 200 | `/api/auth/*` | Identity 5002 |
| 190 | `/api/profile/*` | Identity 5002 |
| 185 | `/api/users/{userId}/ratings` | Event 5003 |
| 180 | `/api/users/*`, `/api/skills/*`, `/api/Roles/*`, `/api/organizer/verification/*`, `/api/uploads/*` | Identity 5002 |
| 175 | `/api/events/{id}/support-campaigns/*`, `/api/events/{id}/sponsors/*`, `/api/events/{id}/sponsorship-proposals/*` | Finance 5004 |
| 172 | `/api/sponsor/profile` | Finance 5004 |
| 170 | `/api/notifications/*`, `/api/badges`, `/api/admin/users/*`, `/api/admin/volunteer-kyc/*`, `/api/admin/organizer-verifications/*`, `/api/admin/monitoring/*`, `/api/admin/audit-logs` | Identity 5002 |
| 160 | `/api/events/*`, `/api/event-categories/*`, `/api/my-registrations`, `/api/certificates/*`, `/api/channels/*`, `/api/ratings/*` | Event 5003 |
| 150 | `/api/dashboard/*` | Event 5003 |
| 140 | `/api/support-campaigns/*`, `/api/donations/*`, `/api/sponsors/*`, `/api/sponsorship-proposals/*`, `/api/admin/finance/*`, `/api/admin/export/finance` | Finance 5004 |
| 130 | `/api/admin/export/events` | API 5001 |
| 130 | `/api/admin/export/users` | Identity 5002 |
| 1 | `/api/*` (fallback) | Legacy 5001 |

## 3. Database — Entity chính

### User
| Field | Type | Ghi chú |
|---|---|---|
| Id | int (PK) | Auto increment |
| UserName | string(50) | Unique |
| Password | string(255) | Hashed, `[JsonIgnore]` |
| Salt | byte[] | `[JsonIgnore]` |
| Name | string(100) | Tên hiển thị |
| Email | string(100) | |
| Phone | string(20) | |
| IsActive | bool | Admin toggle |
| UserType | int | 0=Volunteer, 1=Organizer, 2=Sponsor, 3=Admin |

### Event
| Field | Type | Ghi chú |
|---|---|---|
| Id | int (PK) | |
| Title | string(200) | Bắt buộc |
| Description | string(2000) | |
| Location | string(300) | Địa chỉ text |
| Latitude, Longitude | decimal(9,6) | Tọa độ bản đồ |
| CheckInRadiusKm | decimal(5,2) | Bán kính check-in GPS, mặc định 0.5km |
| StartDate, EndDate | DateTime | |
| MinParticipants | int | Mặc định 1 |
| MaxParticipants | int | |
| CurrentParticipants | int | Counter |
| RequiresKyc | bool | |
| RequiredSkillIds | string(500) | JSON array `"[1,3]"` |
| Status | string(50) | Pending/Approved/Completed/Rejected/Cancelled |
| CategoryId | int (FK) | → EventCategory |
| OrganizerId | int (FK) | → User |
| QrCode | string(500) | GUID-based, sinh khi Approved, hỗ trợ rotate |
| CancelReason | string(1000) | Lý do hủy |
| CancelledAt | DateTime? | |
| RejectReason | string(1000) | Lý do từ chối (≥10 ký tự) |

### Registration
| Field | Type | Ghi chú |
|---|---|---|
| Id | int (PK) | |
| EventId | int (FK) | Unique cùng UserId |
| UserId | int (FK) | |
| ShiftId | int? (FK) | → WorkShift |
| Status | string(50) | Pending/Confirmed/Cancelled |
| RegisteredAt | DateTime | |
| ConfirmedAt | DateTime? | |
| CancelledAt | DateTime? | Thời điểm hủy |
| Note | string(500) | Ghi chú khi đăng ký |
| IsAttended | bool | |
| AttendedAt | DateTime? | Thời điểm check-in |
| CheckedOutAt | DateTime? | Thời điểm check-out |
| VolunteerHours | decimal(5,2) | Pro-rate từ check-in đến check-out |
| CancelRequested | bool | Volunteer xin hủy |
| CancelRequestedAt | DateTime? | |
| CancelReason | string(500) | |

### SponsorProfile
| Field | Type | Ghi chú |
|---|---|---|
| Id | int (PK) | |
| UserId | int (FK, unique) | → User (1-1) |
| OrganizationName | string | Tên tổ chức |
| RepresentativeName | string | Tên đại diện |
| ContactEmail | string | Email liên hệ |
| Phone | string | SĐT |
| Website | string | |
| LogoUrl | string | |
| Description | string | Mô tả |
| IsVerified | bool | |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime | |

### SupportCampaign
| Field | Type | Ghi chú |
|---|---|---|
| Id | int (PK) | |
| EventId | int (FK) | |
| Title | string(200) | |
| Description | string(2000) | |
| TargetAmount | decimal(18,2) | |
| MinimumAmount | decimal? | |
| StartDate, EndDate | DateTime | |
| ReceiveInfo | string(1000) | Thông tin nhận tiền |
| TransparencyNote | string(1000) | Ghi chú minh bạch |
| Status | string(50) | Draft/Open/Closed/Cancelled/Reported |
| CreatedBy | int (FK) | → User |
| UsedAmount | decimal? | Báo cáo sử dụng |
| ReportSummary | string(2000) | |
| ExpenseDetails | string(4000) | |
| ReportedAt | DateTime? | |

### IndividualDonation
| Field | Type | Ghi chú |
|---|---|---|
| Id | int (PK) | |
| CampaignId | int (FK) | |
| UserId | int (FK) | |
| Amount | decimal(18,2) | > 0 |
| DisplayName | string(120) | Bắt buộc nếu không ẩn danh |
| IsAnonymous | bool | |
| ProofImageUrl | string(500) | Ảnh minh chứng |
| Status | string(50) | PendingConfirmation/Confirmed/Rejected/Cancelled |
| RejectedReason | string(500) | Lý do từ chối |

### SponsorshipProposal
| Field | Type | Ghi chú |
|---|---|---|
| Id | int (PK) | |
| EventId | int (FK) | |
| SponsorId | int (FK) | |
| OrganizerId | int (FK) | |
| Type | string(50) | OrganizerRequest / SponsorOffer |
| Title | string(200) | |
| RequestedAmount | decimal? | Cho OrganizerRequest |
| OfferedAmount | decimal? | Cho SponsorOffer |
| ActualReceivedAmount | decimal? | Số tiền thực nhận |
| Status | string(50) | Pending/Accepted/Received/Reported/Rejected/Cancelled |
| UsedAmount | decimal? | Báo cáo sử dụng |

### Các entity khác
- **WorkShift**: ca làm việc (id, eventId, name, startTime, endTime, maxVolunteers, requiredSkillId?)
- **Certificate**: chứng chỉ (id, userId, eventId, certificateCode [unique], volunteerHours, issuedAt, pdfUrl)
- **CertificateJob**: job cấp chứng chỉ (id, eventId, status [Pending/Completed/Failed], createdAt)
- **Badge**: huy hiệu (id, name, description, iconUrl, condition [JSON: min_events, min_hours])
- **UserBadge**: user nhận badge (id, userId, badgeId, awardedAt)
- **Rating**: đánh giá (id, eventId, raterId, rateeId, score 1-5, comment, isHidden, hiddenReason, hiddenAt, hiddenBy)
- **Channel**: kênh trao đổi (id, eventId, parentChannelId?, shiftId?, name, isActive)
- **Post**: bài viết (id, channelId, authorId, content, imageUrl, createdAt)
- **Comment**: bình luận (id, postId, authorId, content, createdAt)
- **Like**: thích (id, postId, userId [unique cùng postId])
- **Notification**: thông báo (id, userId, title, message, type, relatedId, isRead, createdAt)
- **OrganizerVerification**: hồ sơ xác minh (id, organizerId [unique], organizationName, representativeName, contactEmail, phone, address, documentUrl, status, adminNote, rejectReason, verifiedAt, verifiedBy)
- **VolunteerProfile**: hồ sơ volunteer (id, userId [unique], bloodType, languages, interests, bio, avatarUrl, totalVolunteerHours, kycStatus, identityFrontImageUrl, identityBackImageUrl, portraitImageUrl, kycSubmittedAt, kycReviewedAt)
- **VolunteerSkill**: kỹ năng volunteer (id, userId, skillId [unique cùng userId], level, verificationStatus, evidenceUrl, verificationNote, adminNote)
- **AuditLog**: log thao tác (id, userId, action, entityType, entityId, metadata [ghi method/GPS/IP cho check-in], ipAddress, createdAtUtc)
- **EventSponsor**: tài trợ legacy (id, eventId, sponsorId, contributionType, amount, note, sponsoredAt)

## 4. API Endpoint chính

### Identity Service (5002)
```
POST   /api/auth/register                              Đăng ký tài khoản
POST   /api/auth/login                                 Đăng nhập, trả JWT
POST   /api/auth/refresh-token                         Làm mới token

GET    /api/profile                                    Xem hồ sơ bản thân
GET    /api/profile/{userId}                           Xem hồ sơ người khác (public)
PUT    /api/profile                                    Cập nhật hồ sơ
POST   /api/profile/kyc                                Gửi xác minh KYC
GET    /api/profile/passport                           Xem hộ chiếu tình nguyện
POST   /api/profile/skills                             Thêm kỹ năng vào hồ sơ
DELETE /api/profile/skills/{skillId}                    Xóa kỹ năng khỏi hồ sơ
PUT    /api/profile/skills/{skillId}/verification      Gửi minh chứng kỹ năng

GET    /api/skills                                     Danh sách kỹ năng (public)
POST   /api/skills                          [Admin]    Tạo kỹ năng
PUT    /api/skills/{id}                     [Admin]    Sửa kỹ năng
DELETE /api/skills/{id}                     [Admin]    Xóa kỹ năng (cleanup JSON)

POST   /api/organizer/verification                     Gửi hồ sơ xác minh tổ chức
GET    /api/organizer/verification                     Xem trạng thái xác minh

POST   /api/uploads                                    Upload file/ảnh

GET    /api/notifications                              Danh sách thông báo
PUT    /api/notifications/{id}/read                    Đánh dấu đã đọc
PUT    /api/notifications/read-all                     Đánh dấu tất cả đã đọc

GET    /api/badges                                     Danh sách huy hiệu
GET    /api/my-badges                                  Huy hiệu của tôi

POST   /api/admin/users                     [Admin]    Tạo user mới
GET    /api/admin/users                     [Admin]    Danh sách user
PUT    /api/admin/users/{id}/toggle-status  [Admin]    Khóa/mở user (kèm cascade)
GET    /api/admin/volunteer-kyc             [Admin]    Danh sách KYC chờ duyệt
PUT    /api/admin/volunteer-kyc/{id}/approve [Admin]   Duyệt KYC
PUT    /api/admin/volunteer-kyc/{id}/reject  [Admin]   Từ chối KYC
GET    /api/admin/volunteer-skill-verifications [Admin] Danh sách skill chờ duyệt
PUT    /api/admin/volunteer-skill-verifications/{id}/approve [Admin]
PUT    /api/admin/volunteer-skill-verifications/{id}/reject  [Admin]
GET    /api/admin/organizer-verifications   [Admin]    Danh sách organizer chờ duyệt
PUT    /api/admin/organizer-verifications/{id}/approve [Admin]
PUT    /api/admin/organizer-verifications/{id}/reject  [Admin]
GET    /api/admin/monitoring/summary        [Admin]    Health + số liệu hệ thống
GET    /api/admin/audit-logs                [Admin]    Lịch sử thao tác
GET    /api/admin/export/users              [Admin]    Export user JSON/CSV
```

### Event Service (5003)
```
GET    /api/events                                     Danh sách event (public, filter)
GET    /api/events/{id}                                Chi tiết event
GET    /api/events/my                       [Organizer] Event của tôi
GET    /api/events/recommended              [Volunteer] Gợi ý theo kỹ năng
GET    /api/events/overdue-preview          [Admin]    Danh sách event quá hạn chưa complete
POST   /api/events                          [Organizer] Tạo event
PUT    /api/events/{id}                     [Organizer] Sửa event
DELETE /api/events/{id}                     [Owner/Admin] Xóa event
PUT    /api/events/{id}/approve             [Admin]    Duyệt → Approved
PUT    /api/events/{id}/reject              [Admin]    Từ chối → Rejected (reason ≥10 chars)
PUT    /api/events/{id}/complete            [Organizer/Admin] Hoàn thành
PUT    /api/events/{id}/cancel              [Organizer/Admin] Hủy + cascade
POST   /api/events/{id}/resubmit           [Organizer] Gửi duyệt lại
POST   /api/events/{id}/uncomplete         [Admin]    Mở lại (rollback) + notify organizer
POST   /api/events/auto-complete-overdue   [Admin]    Auto-complete quá hạn
PUT    /api/events/{id}/transfer            [Admin]    Chuyển quyền (validate Verified + notify)
POST   /api/events/{id}/qr/rotate          [Organizer/Admin] Xoay mã QR check-in
GET    /api/events/{id}/impact                         Tác động công khai

GET    /api/events/{id}/registrations       [Organizer/Admin] Danh sách đăng ký
GET    /api/events/{id}/my-registration     [Authenticated]   Đăng ký của tôi
POST   /api/events/{id}/register            [Volunteer] Đăng ký
DELETE /api/events/{id}/register            [Volunteer] Rút đăng ký
POST   /api/events/{id}/register/cancel-request [Volunteer] Xin hủy sau confirm
PUT    /api/events/{id}/registrations/{regId}/confirm [Organizer]
PUT    /api/events/{id}/registrations/{regId}/cancel  [Organizer]
POST   /api/events/{id}/registrations/{regId}/checkin [Organizer] QR/GPS
POST   /api/events/{id}/self-checkin        [Volunteer] Tự check-in
POST   /api/events/{id}/walk-in             [Organizer] Đăng ký tại chỗ
POST   /api/events/{id}/registrations/{regId}/manual-attend [Organizer]
POST   /api/events/{id}/registrations/{regId}/checkout [Organizer] Check-out + pro-rate hours
PUT    /api/events/{id}/registrations/{regId}/hours [Organizer] Chỉnh giờ
GET    /api/my-registrations                [Authenticated] Lịch sử đăng ký

GET    /api/events/{id}/shifts                         Danh sách ca
POST   /api/events/{id}/shifts              [Organizer] Tạo ca
PUT    /api/events/{id}/shifts/{shiftId}    [Organizer] Sửa ca
DELETE /api/events/{id}/shifts/{shiftId}    [Organizer] Xóa ca

GET    /api/event-categories                           Danh mục sự kiện
POST   /api/event-categories                [Admin]    Tạo danh mục
PUT    /api/event-categories/{id}           [Admin]    Sửa danh mục
DELETE /api/event-categories/{id}           [Admin]    Xóa danh mục

GET    /api/certificates/{code}                        Xác thực chứng chỉ
GET    /api/certificates/{code}/pdf                    Tải PDF
GET    /api/my-certificates                 [Authenticated]

GET    /api/channels/{id}                   [Authenticated] Xem channel
POST   /api/channels/{id}/posts             [Authenticated] Đăng bài
POST   /api/channels/{id}/posts/{postId}/comments [Authenticated]
POST   /api/channels/{id}/posts/{postId}/likes    [Authenticated]

POST   /api/events/{id}/ratings             [Authenticated] Tạo đánh giá
GET    /api/users/{userId}/ratings                     Xem đánh giá (public)
GET    /api/ratings/admin                   [Admin]    Lọc theo raterId/rateeId/eventId/hidden
PUT    /api/ratings/{id}/hide               [Admin]    Ẩn đánh giá
PUT    /api/ratings/{id}/unhide             [Admin]    Hiện lại
DELETE /api/ratings/{id}                    [Admin]    Xóa đánh giá (chỉ Admin)

GET    /api/dashboard                       [Authenticated] Dashboard theo role
GET    /api/dashboard/organizer-insights    [Organizer] Insights chi tiết
GET    /api/admin/export/events             [Admin]    Export event JSON/CSV (maxRows limit)
```

### Finance Service (5004)
```
GET    /api/sponsor/profile                 [Sponsor]  Xem hồ sơ nhà tài trợ
PUT    /api/sponsor/profile                 [Sponsor]  Cập nhật hồ sơ nhà tài trợ

GET    /api/events/{id}/support-campaigns              Danh sách campaign (public nếu Open)
GET    /api/support-campaigns/{id}                     Chi tiết campaign
POST   /api/events/{id}/support-campaigns   [Organizer] Tạo campaign
PUT    /api/support-campaigns/{id}          [Organizer] Sửa campaign
PUT    /api/support-campaigns/{id}/open     [Organizer] Mở nhận ủng hộ
PUT    /api/support-campaigns/{id}/close    [Organizer] Đóng campaign
PUT    /api/support-campaigns/{id}/cancel   [Organizer] Hủy campaign
POST   /api/support-campaigns/{id}/report   [Organizer] Báo cáo sử dụng tiền

GET    /api/support-campaigns/{id}/donations [Organizer/Admin] Danh sách donation
POST   /api/support-campaigns/{id}/donations [Volunteer]       Gửi ủng hộ
GET    /api/donations/my                    [Authenticated]    Ủng hộ của tôi
PUT    /api/donations/{id}/confirm          [Organizer] Xác nhận đã nhận
PUT    /api/donations/{id}/reject           [Organizer] Từ chối
PUT    /api/donations/{id}/cancel           [Owner/Organizer]  Hủy khi pending

GET    /api/events/{id}/sponsorship-proposals [Organizer/Sponsor/Admin]
POST   /api/events/{id}/sponsorship-proposals/organizer-request [Organizer]
POST   /api/events/{id}/sponsorship-proposals/sponsor-offer [Sponsor] (duplicate prevention)
GET    /api/sponsorship-proposals/my        [Organizer/Sponsor]
PUT    /api/sponsorship-proposals/{id}/accept [Bên nhận]
PUT    /api/sponsorship-proposals/{id}/reject [Bên nhận]
PUT    /api/sponsorship-proposals/{id}/cancel [Bên tạo, khi Pending]
PUT    /api/sponsorship-proposals/{id}/received [Organizer] Xác nhận nhận tiền
PUT    /api/sponsorship-proposals/{id}/admin-revert-to-pending [Admin]
POST   /api/sponsorship-proposals/{id}/report [Organizer] Báo cáo sử dụng

GET    /api/events/{id}/sponsors                       Danh sách sponsor (legacy)
POST   /api/events/{id}/sponsors            [Sponsor]  Tài trợ nhanh (legacy)
GET    /api/sponsors/my                     [Sponsor]  Tài trợ của tôi
GET    /api/sponsors/users                  [Organizer/Admin] DS tài khoản sponsor

GET    /api/sponsors/my/{id}/tracking       [Sponsor]  Tracking tiến độ

GET    /api/admin/finance/overview          [Admin]    Tổng quan tài chính
GET    /api/admin/finance/stale-donations   [Admin]    Donation pending quá hạn
GET    /api/admin/finance/unreported-campaigns [Admin] Campaign chưa báo cáo
GET    /api/admin/finance/open-proposals-past-event [Admin] Proposal kẹt
GET    /api/admin/export/finance            [Admin]    Export finance JSON/CSV (maxRows limit)
```

## 5. Công nghệ sử dụng

| Thành phần | Công nghệ | Version |
|---|---|---|
| Backend | .NET / ASP.NET Core | 8.0 |
| ORM | Entity Framework Core | 8.0.26 |
| Database | SQL Server | 2022 |
| API Gateway | Ocelot | 23.x |
| Auth | JWT Bearer | 8.0.26 |
| Realtime | SignalR | (built-in ASP.NET Core) |
| Frontend | React + Vite | React 18, Vite 5 |
| UI Library | Ant Design / custom CSS | |
| Map | Leaflet | |
| Version Control | Git + GitHub | |
| IDE | Visual Studio / VS Code | |
