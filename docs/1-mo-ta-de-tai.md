# Mô tả đề tài — VolunteerHub

## 1. Bối cảnh

Các hoạt động tình nguyện hiện nay thường được tổ chức rời rạc qua mạng xã hội. Cách làm này gây ra 3 vấn đề chính:

1. **Khó kiểm chứng uy tín tổ chức** — không có cơ chế xác minh pháp lý cho đơn vị tổ chức sự kiện.
2. **Khó kết nối đúng người** — tình nguyện viên có kỹ năng phù hợp không biết sự kiện nào cần mình, và ngược lại.
3. **Đóng góp không được ghi nhận** — tình nguyện viên thiếu bằng chứng chính thống về quá trình tham gia (giờ tình nguyện, chứng chỉ, hồ sơ).

## 2. Giải pháp

VolunteerHub là hệ thống web quản lý hoạt động tình nguyện, tạo ra một hệ sinh thái minh bạch kết nối tổ chức, tình nguyện viên, nhà tài trợ và quản trị viên.

## 3. Mục tiêu

1. **Kết nối đúng người, đúng việc** — dựa trên kỹ năng, vị trí địa lý và nhu cầu sự kiện.
2. **Minh bạch hóa** — thông tin tổ chức, sự kiện, tài trợ và kết quả tác động xã hội đều công khai.
3. **Quản lý trọn vòng đời sự kiện** — từ tạo, duyệt, đăng ký, điểm danh, check-out đến hoàn thành và báo cáo.
4. **Số hóa đóng góp** — tự động cấp chứng chỉ điện tử, ghi nhận giờ tình nguyện (pro-rate theo thời gian thực tế), trao huy hiệu.
5. **Hỗ trợ vận hành tại hiện trường** — giao diện mobile-friendly, điểm danh QR/GPS, check-out ghi nhận giờ thực tế.

## 4. Đối tượng người dùng (Actor)

### 4.1. Tình nguyện viên (Volunteer)

Người dùng cá nhân tham gia hoạt động tình nguyện.

Có thể:
- Tạo hồ sơ cá nhân, khai báo kỹ năng, ngôn ngữ, sở thích.
- Gửi xác minh danh tính (KYC) nếu sự kiện yêu cầu.
- Tìm kiếm và đăng ký sự kiện phù hợp; xem gợi ý sự kiện theo kỹ năng.
- Chọn ca làm việc, rút đăng ký khi chưa được xác nhận.
- Tự điểm danh bằng QR tại hiện trường.
- Xem lịch sử tham gia, giờ tình nguyện, chứng chỉ, huy hiệu.
- Đánh giá nhà tổ chức sau sự kiện.
- Ủng hộ tiền cho đợt kêu gọi của sự kiện.

### 4.2. Nhà tổ chức (Organizer)

Tổ chức hoặc cá nhân chịu trách nhiệm tạo và vận hành sự kiện.

Có thể:
- Gửi hồ sơ xác minh pháp lý tổ chức.
- Tạo sự kiện (mô tả, kỹ năng cần, thời gian, địa điểm, số lượng, yêu cầu KYC, bán kính check-in).
- Quản lý đăng ký: xác nhận, từ chối, điểm danh thủ công, check-out.
- Tạo ca làm việc, quản lý tiến độ tài trợ.
- Hoàn thành sự kiện và xem báo cáo tác động.
- Tạo đợt kêu gọi ủng hộ, xác nhận khoản đã nhận.
- Mời nhà tài trợ hoặc chấp nhận đề nghị tài trợ.
- Đánh giá tình nguyện viên sau sự kiện.
- Xoay mã QR check-in khi cần.

### 4.3. Nhà tài trợ (Sponsor)

Tổ chức hoặc doanh nghiệp tài trợ chính thức cho sự kiện.

Có thể:
- Quản lý hồ sơ nhà tài trợ (SponsorProfile): tên tổ chức, đại diện, email, SĐT, website, logo, mô tả.
- Xem sự kiện đã được duyệt.
- Gửi đề nghị tài trợ hoặc chấp nhận lời mời từ nhà tổ chức.
- Theo dõi trạng thái tài trợ và báo cáo sử dụng tiền.

### 4.4. Quản trị viên (Admin)

Người quản lý toàn hệ thống.

Có thể:
- Duyệt/từ chối hồ sơ xác minh tổ chức (kèm lý do ≥10 ký tự khi từ chối).
- Duyệt/từ chối sự kiện (kèm lý do ≥10 ký tự khi từ chối).
- Duyệt KYC tình nguyện viên (kèm lý do khi từ chối).
- Quản lý người dùng: tìm kiếm, khóa/mở, tạo user mới.
- Quản lý danh mục sự kiện, kỹ năng.
- Xem monitoring, audit log (ghi nhận method/GPS/IP), export dữ liệu.
- Xem admin inbox (KYC chờ, organizer chờ, skill chờ, donation quá hạn, cert job lỗi).
- Xem overdue preview (event quá hạn chưa complete).
- Xử lý tình huống đặc biệt (hủy sự kiện, chuyển quyền, ẩn/xóa đánh giá, xoay QR).

### 4.5. Khách (Guest)

Người chưa đăng nhập.

Có thể:
- Xem landing page, danh sách sự kiện công khai, chi tiết sự kiện đã duyệt.
- Xem kết quả tác động công khai của sự kiện đã hoàn thành.
- Xác thực chứng chỉ bằng mã verify.

## 5. Yêu cầu phi chức năng

| Yêu cầu | Mô tả |
|---|---|
| Bảo mật | JWT + role-based access, không trả password/salt về client, audit log thao tác nhạy cảm (ghi method/GPS/IP), IsActive middleware chặn user bị khóa (401) trên tất cả service |
| Minh bạch | Thông tin event/organizer/sponsor/impact công khai, certificate có verify độc lập |
| Hiệu năng | Pagination/filter cho danh sách lớn, rate limiting cho API nhạy cảm, export giới hạn maxRows (10.000) + CSV injection protection |
| Khả dụng | Responsive desktop + mobile, thao tác tại hiện trường dễ dàng |
| Realtime | SignalR hub cho channel trao đổi sự kiện (EventService) |
| Bảo trì | API contract nhất quán giữa frontend/backend, tài liệu cập nhật khi đổi flow |

## 6. Phạm vi

### Trong phạm vi
- Đăng ký, đăng nhập, phân quyền 4 vai trò.
- Quản lý hồ sơ, kỹ năng, KYC, xác minh tổ chức, hồ sơ nhà tài trợ (SponsorProfile).
- Tạo, duyệt, hủy, hoàn thành sự kiện.
- Đăng ký, chọn ca, rút đăng ký, xin hủy sau xác nhận.
- Điểm danh QR/GPS, walk-in, bổ sung điểm danh, check-out (pro-rate giờ).
- Chứng chỉ tự động, huy hiệu theo điều kiện.
- Đánh giá hai chiều, moderation.
- Ủng hộ cá nhân qua campaign, tài trợ doanh nghiệp qua proposal.
- Dashboard, thông báo, export, monitoring.
- Kiến trúc microservice (4 service + gateway).
- SignalR realtime cho channel sự kiện.

### Ngoài phạm vi
- Thanh toán online thật (tiền giao dịch ngoài hệ thống).
- Phỏng vấn trực tuyến trước khi duyệt volunteer.
- Hợp đồng tài trợ pháp lý.
- Push notification (mobile native).
- Ứng dụng mobile native.
