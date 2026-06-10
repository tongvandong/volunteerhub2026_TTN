# Kế hoạch & tiêu chí đánh giá — Layout thích nghi (auth-aware)

> **Vấn đề gốc:** trang "vừa công khai vừa nội bộ" (sự kiện công khai) trước đây luôn dùng `PublicLayout` → người đã đăng nhập mất sidebar, phải qua nút Dashboard mới sang được mục khác → đứt mạch.
> **Mục tiêu:** trải nghiệm điều hướng **liền mạch** cho người đã đăng nhập, **rõ ràng** cho khách. Đi theo mô hình **adaptive (C)**: cùng URL, layout chọn theo trạng thái đăng nhập.

---

## 1. Nguyên tắc
1. **Một nguồn menu duy nhất theo vai trò** — `utils/roleNav.js` xuất `ROLE_NAV/ROLE_LABEL/ROLE_BADGE`; cả sidebar (MainLayout) và account dropdown (PublicLayout) cùng dùng.
2. **Sidebar luôn theo người dùng** — đã đăng nhập đi đâu trong app đều thấy sidebar quen thuộc.
3. **Khách trải nghiệm thuần marketing/công khai** — header sáng, không lộ tiểu tiết app.
4. **Verify token với BE khi load** — không tin cache mù; BE down → đẩy về `/login` (đã chỉnh `AuthContext`).

---

## 2. Phân loại route

| Loại | Ví dụ | Layout |
|------|-------|--------|
| **Public‑only** (chỉ khách thực sự cần) | `/` (landing), `/verify/check`, `/verify/:code`, `/login`, `/register` | `PublicLayout` |
| **Shared** (chung cho khách + người dùng, là điểm "đứt mạch" cũ) | `/events`, `/events/:id` | `SharedLayout` — auth ⇒ `MainLayout`, khách ⇒ `PublicLayout` |
| **App‑only** | tất cả route có `<AppPage>` (`/dashboard`, `/my-events`, `/admin/*`, `/activity`, `/profile`, `/notifications`, `/channels/:id`, …) | `MainLayout` |

> *(Tùy chọn mở rộng nếu phát sinh nhu cầu)* các trang loại "public utility" khác (vd báo cáo công khai, statistics công khai) cũng có thể chuyển sang `SharedLayout` cùng pattern.

---

## 3. Đã hoàn thành (bám theo hội thoại)
- ✅ Tách `ROLE_NAV/ROLE_LABEL/ROLE_BADGE` ra `utils/roleNav.js`.
- ✅ `PublicLayout`: thay nút "Dashboard" lạc quẻ bằng **account dropdown** (avatar + caret + menu role‑aware + chấm thông báo + đăng xuất).
- ✅ `SharedLayout`: bọc `/events`, `/events/:id` — auth thấy sidebar, khách thấy header công khai.
- ✅ `AuthContext`: bỏ tin cache mù, luôn gọi `authApi.me()` verify với BE.
- ✅ Modal z‑index vượt Leaflet (2000) tránh map đè modal.
- ✅ Badge "việc đang chờ" trên tab quản lý sự kiện + thông báo đẩy khi có ủng hộ mới.

---

## 4. Còn cần làm / cần kiểm chứng

### 4.1 Active state sidebar khi đứng ở route chia sẻ
- Logic `isActive` hiện trong `MainLayout`: `pathname === path || (path !== '/' && pathname.startsWith(path))`.
- Vấn đề: ở `/events/create` thì cả "Sự kiện công khai" (`/events`) lẫn "Tạo sự kiện" (`/events/create`) đều thỏa `startsWith` → highlight cùng lúc.
- **Sửa:** đổi sang khớp chính xác đoạn segment đầu — `pathname === path || pathname.startsWith(path + '/')`.

### 4.2 Flicker layout khi đang xác thực
- Khi `loading=true`, `SharedLayout` tạm hiển thị `PublicLayout`; sau khi xác thực xong chuyển sang `MainLayout` → có nháy.
- **Phương án nhẹ:** hiện `PageLoader` toàn màn hình trong khi `loading=true` (không nháy header). Áp dụng cả `SharedLayout` và cấp `AppRoutes` top.

### 4.3 Khách bấm route app‑only
- Hiện `ProtectedRoute` redirect về `/login`. Sau login → mặc định về `getDefaultRouteByRole(user.role)`. Cần đảm bảo **"return to"** đưa về đúng URL ban đầu khi đăng nhập xong (tùy chọn nâng cao).

### 4.4 Landing `/` cho người đã đăng nhập
- Hiện vẫn `PublicLayout` (landing marketing).
- 2 quan điểm: (a) giữ — landing là điểm thông tin, kể cả user vào cũng OK; (b) redirect `/dashboard` khi đăng nhập.
- **Khuyến nghị:** giữ (a). Đã có account dropdown ở góc phải để vào app.

### 4.5 Mobile
- `MainLayout` đã có sidebar mobile (drawer, hamburger trong topbar). Khi `SharedLayout` chọn `MainLayout` trên mobile → trải nghiệm như app.
- `PublicLayout` mobile có hamburger riêng cho nav công khai và account dropdown vẫn nằm ngoài.
- Cần test: chuyển nhanh giữa /events (auth) và /my-events trên mobile để xác nhận sidebar drawer vẫn dùng được.

### 4.6 Verify performance
- Mỗi lần load app gọi `authApi.me()` → 1 request RTT. Acceptable trong môi trường thường. Nếu BE chậm/đang khởi động, người dùng phải chờ.
- *(Tùy chọn)* cache verify trong 30s — nếu reload nhanh trong 30s thì khỏi verify lại. Không bắt buộc.

### 4.7 Notification badge tươi
- `PublicLayout` đếm unread mỗi khi `pathname` đổi (như `MainLayout`).
- Cần test: từ `/notifications` quay ra `/events` (auth → MainLayout), badge tự về 0 khi đánh dấu đã đọc.

### 4.8 Sạch CSS/UI nhỏ
- Trên `PublicLayout`, đảm bảo dropdown z‑index 1200 đủ trên Leaflet/sticky.
- Email dài trong header dropdown phải truncate, badge vai trò nowrap (đã sửa).

---

## 5. Tiêu chí đánh giá (Acceptance Criteria)

### 5.1 Khách (chưa đăng nhập)
- [ ] Vào `/`, `/events`, `/events/:id`, `/verify/check`, `/login`, `/register` đều thấy `PublicLayout` (header sáng + footer).
- [ ] Header phải có Đăng nhập / Đăng ký (không thấy avatar/dropdown).
- [ ] Vào route app (vd `/dashboard`, `/my-events`) → tự đẩy về `/login`.

### 5.2 Đăng nhập (mọi vai trò)
- [ ] Vào `/events`, `/events/:id` thấy **MainLayout** (sidebar) — không còn header công khai.
- [ ] Sidebar menu đúng vai trò (Volunteer/Organizer/Sponsor/Admin) theo `ROLE_NAV`.
- [ ] Bấm "Sự kiện công khai" trên sidebar khi đang ở mục khác → vào /events, sidebar **vẫn còn**, vẫn thấy đang highlight đúng mục.
- [ ] Bấm "Sự kiện của tôi" (Organizer) khi đang ở /events → chuyển mượt, không "đổi giao diện".
- [ ] Active highlight: chỉ **đúng 1 mục** được highlight tại 1 thời điểm (sau khi sửa 4.1).

### 5.3 Landing `/` khi đã đăng nhập
- [ ] Vẫn dùng `PublicLayout` với account dropdown ở góc phải.
- [ ] Dropdown hiện toàn bộ menu theo vai trò, bấm 1 cú vào mục bất kỳ tới đúng route trong MainLayout.

### 5.4 Verify token với BE
- [ ] BE đang chạy, token hợp lệ → vào app như cũ.
- [ ] BE **không** chạy → reload → đẩy về `/login` (không phải vào "trang trắng" hay trang nội bộ).
- [ ] Token hết hạn / sai → BE trả 401 → đẩy về `/login`.

### 5.5 Account dropdown (PublicLayout)
- [ ] Avatar + tên + caret đúng kiểu pill viền nhạt; chấm đỏ khi có thông báo chưa đọc.
- [ ] Mở dropdown thấy: header (avatar lớn, tên, badge vai trò không xuống dòng, email truncate), menu role‑aware, đăng xuất đỏ.
- [ ] Click ngoài / Esc / chọn mục → đóng. z‑index không bị Leaflet đè.

### 5.6 Modal & overlay
- [ ] Mọi modal (Ủng hộ, Phỏng vấn, Xác nhận…) **không bị bản đồ đè**.
- [ ] Modal nằm trên cả thanh sidebar/topbar.

### 5.7 Việc đang chờ
- [ ] Trang quản lý sự kiện: badge đỏ trên tab "Đăng ký", "Kêu gọi ủng hộ", "Tài trợ doanh nghiệp" hiện đúng số (so với dữ liệu pending).
- [ ] Khi volunteer gửi ủng hộ mới → organizer nhận **thông báo đẩy** (chuông sáng + dropdown +1).

### 5.8 Mobile
- [ ] Trên viewport <768px: /events (auth) hiển thị sidebar dạng drawer (hamburger).
- [ ] PublicLayout mobile: hamburger nav công khai + avatar dropdown đều dùng được.

### 5.9 Hiệu năng / không hồi quy
- [ ] FE build sạch (`npm run build` 0 lỗi).
- [ ] Không có regression rõ rệt thời gian load (verify call < 1s khi BE local).

---

## 6. Phương án kiểm thử (test plan)

### 6.1 Kịch bản chính (smoke)
1. **Khách:** mở `/`, `/events`, `/events/:id`, `/verify/check`, `/login`. Kiểm header công khai.
2. **Đăng nhập Organizer:** chạy đầy đủ luồng:
   - Vào `/dashboard` → /events (qua sidebar) → /my-events (qua sidebar) → /events/create → quay /events. Mỗi bước **sidebar còn**, highlight đúng.
3. **Đăng nhập Volunteer:** vào /events → /activity → /profile. Sidebar liên tục.
4. **Account dropdown ở landing:** đăng nhập rồi vào `/` → bấm avatar → chọn "Tổng quan" → vào dashboard.

### 6.2 Kịch bản BE down
1. Tắt 5 host (hoặc đổi base URL FE). Reload app → tự đẩy `/login`. Không vào được route nội bộ.

### 6.3 Kịch bản token hết hạn
1. Sửa tay localStorage `token` thành chuỗi rác → reload → đẩy `/login`.

### 6.4 Cross‑role spot check
- Login 4 vai trò → mở dropdown ở `/` lần lượt: thấy đúng menu theo vai trò, badge vai trò màu khớp.

### 6.5 Regression
- Modal phỏng vấn trên trang quản lý không bị map mini đè.
- Tab "Đăng ký" badge đỏ khi có người đăng ký mới.

---

## 7. Trạng thái

- **2026-05-28** — Kế hoạch lập + thực hiện mục 3 + 4.1/4.2/4.7/4.5.
- **4.1 Active state chính xác:** ✅ `isActive` chuyển sang `pathname === path || pathname.startsWith(path + '/')` ở cả `MainLayout` và `PublicLayout` dropdown.
- **4.2 Loại flicker layout:** ✅ `AppRoutes` gate toàn cục bằng `PageLoader` khi `loading=true`; `SharedLayout` đơn giản hoá.
- **4.7 Notification badge tươi:** ✅ `Notifications.jsx` dispatch event `volunteerhub:notifications-updated` sau markRead/markAllRead; cả `MainLayout` và `PublicLayout` lắng nghe để refetch unread (vẫn refetch theo pathname).
- **4.5 Mobile QA:** ✅ Dropdown thêm `maxWidth: calc(100vw - 24px)` chống tràn viewport; sidebar drawer + hamburger giữ nguyên hành vi.
- **Còn lại (không bắt buộc):** 4.3 return-to URL, 4.4 redirect / cho user (giữ landing), 4.6 cache verify.
