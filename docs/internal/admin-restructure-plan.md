# Kế hoạch tái cấu trúc khu vực Admin

> **Phạm vi:** Điều hướng (sidebar) + gom route, KHÔNG xóa nghiệp vụ nào.
> **Nguyên tắc:** Mọi component hiện có giữ nguyên 100% logic. Chỉ thêm wrapper + tab + redirect + đổi nhãn.
> **Không thuộc phạm vi:** Ink redesign cho các trang admin (tách thành kế hoạch riêng — xem mục 9).

---

## 1. Vấn đề hiện tại

- **11 component → 14 route → 16 mục sidebar.** Sidebar quá dài, khó quét.
- 4 mục nav (`Nhà tổ chức`, `Tình nguyện viên`, `Nhà tài trợ`, `Tất cả tài khoản`) đều mở **cùng** `AdminUsers`, chỉ khác `initialRoleFilter`.
- 2 trang duyệt hồ sơ (`AdminOrganizerVerifications`, `AdminVolunteerVerifications`) đứng rời, không có "nơi duyệt hồ sơ" thống nhất.
- 3 trang CRUD taxonomy (`AdminCategories`, `AdminSkills`, `AdminBadges`) chiếm 3 mục nav cho việc cấu hình hiếm dùng.
- Hai nhãn "Giám sát" (`AdminMonitoring`) và "Giám sát tài chính" (`AdminFinanceWatch`) dễ nhầm.
- Admin dùng chung `Dashboard.jsx` sơ sài; số liệu tổng quan đang nằm lẫn trong `AdminMonitoring`.

**Kết luận:** Không trang nào thừa về *chức năng*. Vấn đề là *nhóm điều hướng*.

---

## 2. Route map hiện tại

| Route | Component | Nav label |
|-------|-----------|-----------|
| `/admin/events` | AdminEvents | Sự kiện |
| `/admin/organizers` | AdminUsers (role=1) | Nhà tổ chức |
| `/admin/organizer-verifications` | AdminOrganizerVerifications | Duyệt hồ sơ tổ chức |
| `/admin/volunteers` | AdminUsers (role=0) | Tình nguyện viên |
| `/admin/volunteer-verifications` | AdminVolunteerVerifications | KYC & kỹ năng |
| `/admin/sponsors` | AdminUsers (role=2) | Nhà tài trợ |
| `/admin/users` | AdminUsers (all) | Tất cả tài khoản |
| `/admin/categories` | AdminCategories | Danh mục sự kiện |
| `/admin/skills` | AdminSkills | Danh mục kỹ năng |
| `/admin/ratings` | AdminRatings | Đánh giá |
| `/admin/badges` | AdminBadges | Huy hiệu |
| `/admin/finance` | AdminFinanceWatch | Giám sát tài chính |
| `/admin/monitoring` | AdminMonitoring | Giám sát |
| `/admin/export` | AdminExport | Xuất dữ liệu |

---

## 3. Cấu trúc đích

**Sidebar: 16 mục → 9 mục.**

| # | Nav label đích | Route | Ghi chú |
|---|----------------|-------|---------|
| 1 | Tổng quan | `/dashboard` | (tùy chọn nâng cấp — Đợt E) |
| 2 | Sự kiện | `/admin/events` | giữ nguyên |
| 3 | Tài khoản | `/admin/users` | **tab role** (Đợt A) |
| 4 | Duyệt hồ sơ | `/admin/verifications` | **wrapper tab** (Đợt B) |
| 5 | Đánh giá | `/admin/ratings` | giữ nguyên |
| 6 | Danh mục hệ thống | `/admin/catalog` | **wrapper tab** (Đợt C) |
| 7 | Đối soát tài chính | `/admin/finance` | đổi nhãn (Đợt D) |
| 8 | Giám sát hệ thống | `/admin/monitoring` | đổi nhãn (Đợt D) |
| 9 | Xuất dữ liệu | `/admin/export` | giữ nguyên |
| — | Thông báo | `/notifications` | giữ nguyên |

Tất cả route cũ vẫn sống nhờ redirect (mục 7) → không vỡ bookmark/link nội bộ.

---

## 4. Đợt A — Gộp 4 mục tài khoản thành 1 (tab role)

### A.1 Mục tiêu
Một mục nav "Tài khoản" duy nhất; tab nội bộ lọc theo role; deep-link `?tab=`.

### A.2 Thay đổi `AdminUsers.jsx`
- Thêm `useSearchParams` đọc/ghi `?tab=`.
- Thêm hàng `Tabs` ánh xạ tab → `roleFilter`:

```jsx
import { useSearchParams } from 'react-router-dom';
import Tabs from '../../components/ui/Tabs';

const ROLE_TABS = [
  { key: 'all',        label: 'Tất cả',          role: '' },
  { key: 'organizers', label: 'Nhà tổ chức',     role: '1' },
  { key: 'volunteers', label: 'Tình nguyện viên', role: '0' },
  { key: 'sponsors',   label: 'Nhà tài trợ',     role: '2' },
];
```

- Trong component: nếu **không** có prop `initialRoleFilter` (tức vào qua `/admin/users`), hiển thị `Tabs`; tab điều khiển `roleFilter` + cập nhật `?tab=`. Khi có prop (các route legacy còn dùng tạm) thì khóa như cũ.
- Bỏ `<select>` lọc role khỏi thanh filter khi đã có tab (tránh trùng); giữ search + status filter.
- `title` mặc định đổi thành **"Tài khoản"**.

### A.3 Sidebar
Xóa 3 mục `Nhà tổ chức / Tình nguyện viên / Nhà tài trợ`; đổi `Tất cả tài khoản` → **"Tài khoản"** trỏ `/admin/users`.

### A.4 Redirect (App.jsx)
```jsx
<Route path="/admin/organizers" element={<Navigate to="/admin/users?tab=organizers" replace />} />
<Route path="/admin/volunteers" element={<Navigate to="/admin/users?tab=volunteers" replace />} />
<Route path="/admin/sponsors"   element={<Navigate to="/admin/users?tab=sponsors" replace />} />
```
(Xóa 3 route `<AppPage>` cũ tương ứng.)

### A.5 Acceptance
- [ ] `/admin/users` có 4 tab, đổi tab đổi `?tab=` và filter đúng role.
- [ ] `/admin/users?tab=organizers` mở thẳng tab tổ chức.
- [ ] 3 link cũ redirect đúng.
- [ ] Sidebar còn 1 mục tài khoản.

---

## 5. Đợt B — Gộp 2 trang duyệt hồ sơ dưới 1 route cha

### B.1 Mục tiêu
1 mục nav "Duyệt hồ sơ" → `/admin/verifications` với 2 tab. **Không refactor logic** 2 component con.

### B.2 Component mới `AdminVerifications.jsx`
```jsx
import { useSearchParams } from 'react-router-dom';
import Tabs from '../../components/ui/Tabs';
import AdminOrganizerVerifications from './AdminOrganizerVerifications';
import AdminVolunteerVerifications from './AdminVolunteerVerifications';

const TABS = [
  { key: 'organizers', label: 'Tổ chức' },
  { key: 'volunteers', label: 'Tình nguyện viên (KYC & kỹ năng)' },
];

export default function AdminVerifications() {
  const [params, setParams] = useSearchParams();
  const tab = params.get('tab') || 'organizers';
  return (
    <div className="space-y-4">
      <div>
        <h1 ...>Duyệt hồ sơ</h1>
        <p ...>Xét duyệt hồ sơ tổ chức và xác minh KYC/kỹ năng tình nguyện viên.</p>
      </div>
      <Tabs tabs={TABS} value={tab} onChange={(k) => setParams({ tab: k })} />
      {tab === 'organizers' ? <AdminOrganizerVerifications embedded /> : <AdminVolunteerVerifications embedded />}
    </div>
  );
}
```
- `AdminVolunteerVerifications` đã có sẵn subtab KYC/Kỹ năng → giữ nguyên, chạy trong tab "Tình nguyện viên".
- Thêm prop `embedded` (mặc định false) cho 2 component con để ẩn `<h1>` tiêu đề riêng khi nhúng (tránh tiêu đề lặp). Thay đổi tối thiểu: bọc phần header bằng `{!embedded && (...)}`.
- *(Tùy chọn)* truyền `count` cho `Tabs` = số hồ sơ chờ duyệt mỗi loại (2 component đã tính sẵn pending count — có thể nâng lên state cha sau, không bắt buộc đợt này).

### B.3 Route + redirect
```jsx
<Route path="/admin/verifications" element={<AppPage roles={['Admin']}><AdminVerifications /></AppPage>} />
<Route path="/admin/organizer-verifications" element={<Navigate to="/admin/verifications?tab=organizers" replace />} />
<Route path="/admin/volunteer-verifications" element={<Navigate to="/admin/verifications?tab=volunteers" replace />} />
```

### B.4 Sidebar
2 mục `Duyệt hồ sơ tổ chức` + `KYC & kỹ năng` → 1 mục **"Duyệt hồ sơ"** (`/admin/verifications`, icon `fa-clipboard-check`).

### B.5 Acceptance
- [ ] `/admin/verifications` hiển thị 2 tab, tab volunteer vẫn có subtab KYC/Kỹ năng.
- [ ] Không lặp tiêu đề `<h1>`.
- [ ] Approve/reject/requestChanges hoạt động y như trước ở từng tab.
- [ ] 2 link cũ redirect đúng.

---

## 6. Đợt C — Gộp Categories/Skills/Badges thành "Danh mục hệ thống"

### C.1 Component mới `AdminCatalog.jsx`
```jsx
const TABS = [
  { key: 'categories', label: 'Danh mục sự kiện' },
  { key: 'skills',     label: 'Kỹ năng' },
  { key: 'badges',     label: 'Huy hiệu' },
];
// render AdminCategories / AdminSkills / AdminBadges theo tab, dùng ?tab=
```
- 3 component con nhận prop `embedded` để ẩn header riêng (như Đợt B).

### C.2 Route + redirect
```jsx
<Route path="/admin/catalog" element={<AppPage roles={['Admin']}><AdminCatalog /></AppPage>} />
<Route path="/admin/categories" element={<Navigate to="/admin/catalog?tab=categories" replace />} />
<Route path="/admin/skills"     element={<Navigate to="/admin/catalog?tab=skills" replace />} />
<Route path="/admin/badges"     element={<Navigate to="/admin/catalog?tab=badges" replace />} />
```

### C.3 Sidebar
3 mục → 1 mục **"Danh mục hệ thống"** (`/admin/catalog`, icon `fa-sliders`).

### C.4 Acceptance
- [ ] 3 tab CRUD hoạt động độc lập, đầy đủ create/edit/delete.
- [ ] Redirect 3 link cũ đúng.

---

## 7. Đợt D — Đổi nhãn cho rõ nghĩa

Chỉ sửa text, không đổi logic.

| Nơi | Cũ | Mới |
|-----|----|-----|
| Sidebar + `<h1>` AdminFinanceWatch | Giám sát tài chính | **Đối soát tài chính** |
| Sidebar + `<h1>` AdminMonitoring | Giám sát | **Giám sát hệ thống** |

---

## 8. Đợt E — (Tùy chọn) Trang Tổng quan Admin

Hiện admin dùng nhánh `isAdmin()` trong `Dashboard.jsx` (5 StatCard). Đề xuất nâng cấp **trong chính nhánh đó** (không thêm route):
- Hàng KPI giữ nguyên (users/events/registrations/certificates).
- Thêm khối **"Cần xử lý"**: số sự kiện chờ duyệt → link `/admin/events?status=Pending`; hồ sơ tổ chức chờ → `/admin/verifications?tab=organizers`; KYC chờ → `/admin/verifications?tab=volunteers`; khoản tài chính treo → `/admin/finance`.
- Dùng dữ liệu `dashboardApi.get()` sẵn có; nếu thiếu count thì lấy thêm từ các API pending đã có.

> Đợt E độc lập, có thể làm sau cùng hoặc bỏ qua.

---

## 9. Ngoài phạm vi — Ink redesign trang admin

Các trang admin vẫn dùng tone cũ (`bg-blue-100`, `text-gray-*`, gradient…). Việc ink-hóa giống organizer/volunteer là **kế hoạch riêng**, không gộp vào đợt tái cấu trúc này để giảm rủi ro. Sau khi gom nav xong sẽ dễ redesign theo từng trang gộp.

---

## 10. Sequencing & rủi ro

| Đợt | Việc | File đụng tới | Rủi ro |
|-----|------|---------------|--------|
| **A** | Tab tài khoản | AdminUsers.jsx, App.jsx, MainLayout.jsx | Thấp |
| **B** | Wrapper duyệt hồ sơ | +AdminVerifications.jsx, 2 con (prop embedded), App.jsx, MainLayout.jsx | Thấp–TB |
| **C** | Wrapper catalog | +AdminCatalog.jsx, 3 con (prop embedded), App.jsx, MainLayout.jsx | Thấp |
| **D** | Đổi nhãn | MainLayout.jsx, 2 page header | Rất thấp |
| **E** | Admin overview | Dashboard.jsx | TB (tùy chọn) |

**Thứ tự đề xuất:** D → A → C → B → (E).
Lý do: D là đổi text 1 phút; A gọn nav nhiều nhất; C đơn giản (CRUD thuần); B cần thêm prop `embedded`; E tùy chọn để cuối.

---

## 11. Nguyên tắc kỹ thuật chung

- Mỗi wrapper chỉ **import + chuyển tab**, không sửa logic con.
- Prop `embedded` ở component con: chỉ bọc phần `<h1>/mô tả` bằng `{!embedded && (...)}`.
- Deep-link tab qua `useSearchParams` (`?tab=`), khớp pattern đã dùng (`/activity?tab=donations`).
- Mọi route cũ → `<Navigate replace>` giữ tương thích.
- Component `Tabs` (`components/ui/Tabs.jsx`) dùng lại, không tạo mới.

---

## 12. Trạng thái

- **2026-05-27** — Lập kế hoạch xong.
- **2026-05-27** — Triển khai theo thứ tự D → A → C → B. Build sạch sau mỗi đợt.
- **Đợt D (Đổi nhãn):** ✅ Sidebar + header: "Đối soát tài chính", "Giám sát hệ thống".
- **Đợt A (Tab tài khoản):** ✅ AdminUsers thêm `ROLE_TABS` + `?tab=` (chỉ khi vào `/admin/users`), ẩn select role khi có tab; sidebar 4 mục → 1 ("Tài khoản"); redirect `/admin/organizers|volunteers|sponsors` → `/admin/users?tab=`.
- **Đợt C (Wrapper catalog):** ✅ Tạo `AdminCatalog` (3 tab); prop `embedded` cho AdminCategories/AdminSkills/AdminBadges (ẩn h1, giữ nút action); sidebar 3 mục → 1 ("Danh mục hệ thống"); redirect 3 route cũ.
- **Đợt B (Wrapper duyệt hồ sơ):** ✅ Tạo `AdminVerifications` (2 tab); prop `embedded` cho 2 trang con (tab volunteer giữ subtab KYC/Kỹ năng); sidebar 2 mục → 1 ("Duyệt hồ sơ"); redirect 2 route cũ.
- **Đợt E (Admin overview — tùy chọn):** ⬜ Chưa làm (ngoài yêu cầu D→A→C→B).

**Kết quả:** Sidebar admin **16 → 9 mục**. Không bỏ chức năng nào; mọi route cũ vẫn redirect đúng.
