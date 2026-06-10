# Kế hoạch redesign trang sự kiện công khai

> **Phạm vi:** `/events` (EventList) + `/events/:id` (EventDetail)
> **Phong cách:** Tiếp tục hệ thống đã chốt cho volunteer — Modern Minimal / ink color system.
> **Nguyên tắc:** Mọi thứ đã build cho volunteer pages (SectionLabel, EmptyState, Tabs, EventCardLarge, ink tokens) được tái sử dụng — không tạo thêm pattern mới.

---

## 1. Vấn đề hiện tại

### 1.1 EventList (`/events`)

| Vấn đề | Biểu hiện cụ thể |
|--------|-----------------|
| **Hero tối màu 460px** | Full-bleed photo + gradient overlay `rgba(12,35,90,0.92)` — quá nặng, phong cách khác hoàn toàn với trang Volunteer |
| **Màu cũ (#181d26)** | Sidebar header, stats bar, `UpcomingRow` dùng `rgba(4,14,32,...)` thay vì ink system `rgba(15,15,15,...)` |
| **Sidebar 300px cố định** | Ẩn hoàn toàn trên mobile; `SidebarCard` dark header không nhất quán; nội dung trong sidebar (upcoming events) không đủ giá trị để chiếm 300px |
| **DateBadge màu sắc** | Mỗi tháng 1 màu trong `MONTH_COLORS` — quá colorful, không minimal |
| **Category tabs tách rời** | Sticky bar riêng (top: 60px) tách khỏi filter bar → 2 row sticky liền nhau gây confusion |
| **Stats bar nền tối cuối trang** | `background: '#181d26'` với số liệu hardcoded ("1.200+", "48+") — không thật, không đồng nhất |
| **Empty state tự chế** | Inline div + icon thay vì dùng `EmptyState` component đã có |
| **Search bar duplicate** | Search trong hero + search trong filter bar — 2 input cùng chức năng |

### 1.2 EventDetail (`/events/:id`)

| Vấn đề | Biểu hiện cụ thể |
|--------|-----------------|
| **MobileActionBar chưa dùng** | Component đã import nhưng chưa render → mobile không có sticky CTA |
| **Không có map vị trí** | Có `Latitude` + `Longitude` + `Location` string nhưng không hiển thị bản đồ |
| **Organizer block thiếu fallback** | Nếu `organizerProfile.avatarUrl` rỗng → vòng tròn rỗng không có initials |
| **Import StatusBadge cũ** | Line 6: `import StatusBadge` — component không còn dùng sau Đợt 4 (dead import) |
| **Một số inline style cũ** | Rải rác `rgba(4,14,32,...)`, hardcoded `#fff`, `#e5e7eb` thay vì ink system |

---

## 2. Design tokens (giữ nguyên từ volunteer redesign)

| Token | Giá trị |
|-------|---------|
| Text primary | `#0F0F0F` |
| Text secondary | `rgba(15,15,15,0.55)` |
| Text tertiary | `rgba(15,15,15,0.35)` |
| Border | `1px solid rgba(15,15,15,0.08)` |
| Border hover | `rgba(15,15,15,0.18)` |
| BG canvas | `#FAFAFA` |
| BG card | `#FFFFFF` |
| Accent | `#1b61c9` |
| Success | `#15803d` |
| Warning | `#b45309` |

---

## 3. Đợt A — EventList redesign

### 3.1 Layout mới (bỏ sidebar, full-width)

```
┌─────────────────────────────────── max-w-6xl ───────────────────────────────────┐
│                                                                                  │
│  ◯ KHÁM PHÁ SỰ KIỆN               ← SectionLabel (uppercase 11px, ink tertiary) │
│  Sự kiện tình nguyện              ← H1 28px font-medium #0F0F0F                 │
│  423 sự kiện đang mở              ← caption 13px ink secondary                  │
│                                                                                  │
├──────────────────────────── Sticky filter bar ──────────────────────────────────┤
│ [🔍 Tìm kiếm sự kiện...]  Tất cả · Môi trường · Y tế · Giáo dục · Cộng đồng… │
│                            ← category chips horizontal scroll (pill, ink style)  │
│                     [Lọc ▾ 1]  [⊞ Grid]  [🗺 Map]                              │
│ ↳ expandable panel: Kỹ năng ▾  Trạng thái ▾  📍 Gần tôi  Bán kính ▾  Xóa lọc │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ✦ Gợi ý cho bạn     [Xem tất cả →]   ← SectionLabel + right link (volunteer)  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  → horizontal snap scroll  │
│  │[img 16:9]│ │[img 16:9]│ │[img 16:9]│ │[img 16:9]│                            │
│  │ Title    │ │ Title    │ │ Title    │ │ Title    │                            │
│  │ 📅 ngày  │ │ 📅 ngày  │ │ 📅 ngày  │ │ 📅 ngày  │                            │
│  │ ✦ 80%   │ │ ✦ 65%   │ │ ✦ 50%   │ │ ✦ 40%   │                            │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘                            │
│                                                                                  │
│  Tất cả sự kiện                          ← SectionLabel                         │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  ← 3 cols desktop           │
│  │ [img 16:9]   │ │ [img 16:9]   │ │ [img 16:9]   │    2 cols tablet            │
│  │ EventCardLarge│ │ EventCardLarge│ │ EventCardLarge│    1 col mobile             │
│  └──────────────┘ └──────────────┘ └──────────────┘                            │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                            │
│  │              │ │              │ │              │                            │
│  └──────────────┘ └──────────────┘ └──────────────┘                            │
│                                                                                  │
│                      ← 1  2  3  4  5 →                                          │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Phần bỏ

| Xóa | Lý do |
|-----|-------|
| **Hero section** (lines 228–294) | Duplicate search, dark overlay không phù hợp ink style |
| **SidebarCard** sub-component | Sidebar bị loại bỏ |
| **UpcomingRow** sub-component | Sidebar bị loại bỏ |
| **DateBadge** sub-component | Sidebar bị loại bỏ |
| **Stats bar** (lines 668–688) | Số liệu hardcoded, màu nền `#181d26` không nhất quán |
| **Sidebar div** (lines 607–664) | Khoảng 300px trả lại cho grid events |

### 3.3 Page header (thay hero)

```jsx
<div className="max-w-6xl mx-auto px-5 sm:px-8 pt-8 pb-4">
  <SectionLabel>Khám phá sự kiện</SectionLabel>
  <div className="flex items-end justify-between gap-4 mt-2">
    <div>
      <h1 className="text-[28px] font-medium tracking-tight" style={{ color: '#0F0F0F' }}>
        Sự kiện tình nguyện
      </h1>
      <p className="text-[13px] mt-1" style={{ color: 'rgba(15,15,15,0.50)' }}>
        {loading ? '…' : `${totalCount} sự kiện đang mở`}
      </p>
    </div>
    {/* View toggle (grid/map) — moved here from filter bar */}
    <div className="flex gap-1.5">
      <ViewToggleBtn mode="grid" ... />
      <ViewToggleBtn mode="map" ... />
    </div>
  </div>
</div>
```

### 3.4 Unified sticky filter bar (category chips + search + filter)

Gộp category tabs + filter bar thành **1 row sticky** duy nhất:

```
┌─────────────────────────────────────────────────────────────────────┐
│ [🔍 Tìm kiếm...]  | Tất cả  Môi trường  Y tế  Giáo dục … |  [Lọc ▾]│
└─────────────────────────────────────────────────────────────────────┘
         flex-1          ← chips scroll →          flexShrink-0
```

**Spec:**
- `position: sticky; top: 60px; z-index: 20` (topbar là 60px)
- `background: #fff; border-bottom: 1px solid rgba(15,15,15,0.08)`
- Padding: `px-5 sm:px-8 py-2.5`
- Search: `input-field` class, `flex-shrink-0 w-48 sm:w-60`
- Separator: `|` hoặc `border-l border-gray-200 mx-2`
- Category chips: horizontal scroll, `scrollbar-width: none`; mỗi chip:
  - Active: `bg-gray-900 text-white`
  - Inactive: `text-gray-600 hover:bg-gray-100`
  - Pill shape: `rounded-full px-3 py-1 text-[13px] font-medium`
- Nút Lọc: giữ logic filter toggle hiện tại, với badge count
- Expandable panel: giữ nguyên (skill, status, geo, radius, clear)

### 3.5 Section "Gợi ý cho bạn" (volunteer only)

```jsx
{isAuthenticated && user?.role === 'Volunteer' && recommended.length > 0 && (
  <section className="max-w-6xl mx-auto px-5 sm:px-8 mb-8">
    <SectionLabel action={<Link to="/events" className="link-inline">Xem tất cả →</Link>}>
      Gợi ý cho bạn
    </SectionLabel>
    <p className="text-[12px] mb-3" style={{ color: 'rgba(15,15,15,0.45)' }}>
      Dựa trên kỹ năng trong hồ sơ của bạn
    </p>
    <div className="flex gap-3 overflow-x-auto snap-x snap-mandatory no-scrollbar pb-1">
      {recommended.map(ev => (
        <EventCardCompact key={ev.id} event={ev} matchPct={ev.matchPercentage ?? ev.matchPct ?? null} />
      ))}
    </div>
  </section>
)}
```

> **Dùng `EventCardCompact`** (w-[232px], 16:9, horizontal scroll) thay vì `EventCardLarge` cho section gợi ý — phân biệt rõ với grid bên dưới.

### 3.6 Section "Tất cả sự kiện"

```jsx
<section className="max-w-6xl mx-auto px-5 sm:px-8 pb-12">
  <SectionLabel className="mb-4">Tất cả sự kiện</SectionLabel>

  {/* Map view */}
  {viewMode === 'map' && <Suspense>…<MapView /></Suspense>}

  {/* Empty state */}
  {!loading && events.length === 0 && (
    <EmptyState
      icon="fa-calendar-xmark"
      title="Không tìm thấy sự kiện phù hợp"
      description="Thử thay đổi bộ lọc hoặc tìm kiếm từ khóa khác."
      cta="Xóa bộ lọc"
      onCtaClick={() => { setFilters(…); setActiveTab(''); }}
    />
  )}

  {/* Grid */}
  {viewMode === 'grid' && !loading && (
    <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
      {(radiusEvents ?? events).map(ev => (
        <EventCardLarge key={ev.id} event={ev} />
      ))}
    </div>
  )}

  {/* Pagination */}
  {viewMode === 'grid' && radiusEvents == null && (
    <div className="mt-8">
      <Pagination … />
    </div>
  )}
</section>
```

### 3.7 Files thay đổi — Đợt A

| File | Thay đổi |
|------|----------|
| `src/pages/public/EventList.jsx` | **Rewrite** — xóa hero/sidebar/stats, layout mới, unified filter bar, SectionLabel, EmptyState |

Không cần tạo component mới — tái dụng hết.

### 3.8 Acceptance criteria — Đợt A

- [ ] Không còn dark hero (`rgba(12,35,90,...)`, `#181d26`)
- [ ] Không còn sidebar 300px
- [ ] Category chips và search nằm trong cùng 1 sticky bar
- [ ] Section "Gợi ý" dùng `EventCardCompact`, horizontal scroll
- [ ] Section "Tất cả" dùng `EventCardLarge`, 3-col grid
- [ ] Empty state dùng component `EmptyState`
- [ ] Map view vẫn hoạt động
- [ ] Geo / radius filter vẫn hoạt động
- [ ] Build clean (0 lỗi)
- [ ] Mobile (< 640px): 1-col grid, filter bar wrap tốt

---

## 4. Đợt B — EventDetail polish

### 4.1 MobileActionBar — wire up sticky CTA

Component `MobileActionBar` đã có nhưng chưa render. Cần thêm vào cuối JSX:

**Logic CTA theo trạng thái:**

| Trạng thái | CTA hiển thị | Màu |
|-----------|--------------|-----|
| Chưa đăng ký + event `Approved` + volunteer | "Đăng ký tham gia" | Primary (`#0F0F0F`) |
| Đã đăng ký `Confirmed` + gần giờ (±2h) | "Điểm danh QR" | Amber |
| Đã đăng ký `Confirmed` + chưa đến giờ | "Xem đăng ký của tôi" | Secondary |
| Đã đăng ký `Pending` | "Đang chờ xác nhận" | Disabled |
| Đã tham gia (`isAttended`) | "Đã điểm danh ✓" | Green disabled |
| Đã hủy hoặc `Completed` | Ẩn bar | — |
| Sponsor | "Ủng hộ sự kiện" | Primary |
| Chưa đăng nhập | "Đăng nhập để đăng ký" | Primary |

```jsx
{/* Bottom of JSX, before closing div */}
<MobileActionBar
  label={mobileCtaLabel}
  onClick={mobileCtaAction}
  disabled={mobileCtaDisabled}
/>
```

### 4.2 Location map section

Nếu event có `latitude` + `longitude`: hiển thị mini `MapView` bên dưới phần thông tin, trước registration card.

```jsx
{event.latitude && event.longitude && (
  <div className="mt-6 overflow-hidden rounded-xl border" style={{ borderColor: 'rgba(15,15,15,0.08)' }}>
    <div className="flex items-center gap-2 px-4 py-3 border-b" style={{ borderColor: 'rgba(15,15,15,0.06)' }}>
      <i className="fa-solid fa-location-dot text-[13px]" style={{ color: '#1b61c9' }} />
      <span className="text-[13px] font-medium" style={{ color: '#0F0F0F' }}>{event.location}</span>
      <a
        href={`https://www.google.com/maps?q=${event.latitude},${event.longitude}`}
        target="_blank" rel="noreferrer"
        className="ml-auto text-[12px] link-inline"
      >
        Mở Google Maps →
      </a>
    </div>
    <Suspense fallback={<div className="h-48 bg-gray-50 animate-pulse" />}>
      <MapView
        events={[event]}
        height={200}
        interactive={false}
      />
    </Suspense>
  </div>
)}
```

### 4.3 Organizer avatar fallback (initials)

Khi `organizerProfile.avatarUrl` rỗng → hiện initials thay vì vòng tròn trắng:

```jsx
{organizerProfile.avatarUrl ? (
  <img src={organizerProfile.avatarUrl} alt="" className="w-10 h-10 rounded-full object-cover" />
) : (
  <div
    className="w-10 h-10 rounded-full flex items-center justify-center text-sm font-semibold flex-shrink-0"
    style={{ background: 'rgba(27,97,201,0.10)', color: '#1b61c9' }}
  >
    {(event.organizer || organizerProfile.name || 'O').charAt(0).toUpperCase()}
  </div>
)}
```

### 4.4 Cleanup còn lại

- Xóa `import StatusBadge` (line 6, dead import)
- Thay `rgba(4,14,32,...)` → `rgba(15,15,15,...)`
- `border: '1px solid #e5e7eb'` → `border: '1px solid rgba(15,15,15,0.08)'`
- Các `color: '#181d26'` → `color: '#0F0F0F'`
- `boxShadow` không cần thiết → bỏ hoặc dùng border

### 4.5 Files thay đổi — Đợt B

| File | Thay đổi |
|------|----------|
| `src/pages/public/EventDetail.jsx` | Wire MobileActionBar, thêm map section, organizer initials fallback, clean up cũ |

### 4.6 Acceptance criteria — Đợt B

- [ ] Mobile: sticky bottom bar hiện CTA đúng theo trạng thái đăng ký
- [ ] Nếu event có tọa độ: hiển thị mini map + link Google Maps
- [ ] Organizer block có initials fallback khi không có avatar
- [ ] Không còn `import StatusBadge` dead import
- [ ] Không còn `rgba(4,14,32,...)`, `#181d26`, `#e5e7eb` hardcoded
- [ ] Build clean

---

## 5. Sequencing đề xuất

| Đợt | File | Thời lượng ước tính | Rủi ro |
|-----|------|--------------------|----|
| **A** | `EventList.jsx` rewrite | ~1 phiên | Layout break mobile nếu không test — test kỹ responsive |
| **B** | `EventDetail.jsx` polish | ~0.5 phiên | MobileActionBar logic phức tạp — cần test từng trạng thái |

---

## 6. Trạng thái

- **2026-05-27** — Kế hoạch được duyệt. Bắt đầu thực hiện.
- **Đợt A (EventList):** ✅ Hoàn thành — Xóa hero/sidebar/stats, thêm page header + sticky filter bar (search + category chips + Lọc panel), section "Gợi ý cho bạn" dùng EventCardCompact, section "Tất cả sự kiện" dùng EventCardLarge 3-col grid, EmptyState component.
- **Đợt B (EventDetail):** ✅ Hoàn thành — Thêm lazy MapView + location mini-map section, organizer initials fallback, fix rgba(4,14,32,…) → rgba(15,15,15,…) và #181d26 → #0F0F0F. MobileActionBar đã được wire từ trước.
