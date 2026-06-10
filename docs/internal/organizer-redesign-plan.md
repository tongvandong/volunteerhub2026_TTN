# Kế hoạch redesign trang Organizer

> **Phạm vi:** 12 file trong `src/pages/organizer/`
> **Nguyên tắc:** Giữ nguyên 100% nghiệp vụ và chức năng — chỉ thay đổi visual layer.
> **Phong cách:** Kế thừa hệ thống đã chốt cho volunteer + public events — Modern Minimal, ink color system.

---

## 1. Tổng quan vấn đề hiện tại

### 1.1 MyEvents (186 dòng)

| Vấn đề | Biểu hiện |
|--------|-----------|
| 6 filter-cards màu sắc | `bg-yellow-50 border-yellow-200`, `bg-green-50`, `bg-red-50`… — quá colorful, không nhất quán |
| Empty state tự chế | `div > icon + p` thay vì `EmptyState` component |
| Màu text | `text-gray-900`, `text-gray-400`, `text-gray-500` — không đồng nhất với ink system |
| Event row thiếu hierarchy | Thumbnail 64×64px nhỏ, title + meta cùng cỡ chữ |

### 1.2 ManageEvent/index.jsx (1103 dòng — orchestrator)

| Vấn đề | Biểu hiện |
|--------|-----------|
| 3 summary cards màu | `bg-yellow-50`, `bg-green-50`, `bg-primary-50` với colored text |
| Progress block xanh | `border-blue-100 bg-blue-50 p-4` — hardcoded blue tint |
| Header thiếu thumbnail | Chỉ có text title, không có hình ảnh sự kiện |
| Tab bar inline style | Chưa dùng `Tabs` component, style riêng |
| Màu cũ rải rác | `text-gray-*`, `ring-blue-*`, `bg-primary-*` không nhất quán |

### 1.3 ManageEvent tabs (6 file, ~1309 dòng tổng)

| File | Vấn đề chính |
|------|-------------|
| RegistrationsTab (247) | Empty state tự chế; warning banner `bg-amber-50 border-amber-200`; table header `table-header` class chưa ink |
| ShiftsTab (140) | Inline style card colors |
| CheckInTab (226) | Inline style mixed với Tailwind |
| CampaignsTab (288) | Colored status badges inline; empty state tự chế |
| CorporateTab (269) | Colored status indicators; empty state tự chế |
| ReportTab (139) | Nhỏ, tương đối sạch nhưng cần ink system |

### 1.4 OrganizerInsights (427 dòng)

| Vấn đề | Biểu hiện |
|--------|-----------|
| Gradient StatCard | `linear-gradient(135deg, #2563eb, #4f46e5)` — không nhất quán với ink system |
| Slate color scale | `text-slate-700`, `text-slate-950`, `bg-slate-100`, `ring-slate-200` — cần chuyển sang ink |
| Empty state tự chế | `EmptyPanel` component riêng thay vì `EmptyState` |

### 1.5 EventForm (993 dòng)

| Vấn đề | Biểu hiện |
|--------|-----------|
| Section headers | `h2 className="text-lg font-semibold text-gray-900"` — nên dùng SectionLabel |
| Step indicator | Tự chế, không nhất quán |
| "Cần xác minh" block | `bg-amber-50 text-amber-600` hardcoded |
| Label text | `text-gray-700`, `text-gray-500`, `text-gray-900` — ink system |

### 1.6 OrganizerVerification (281 dòng)

| Vấn đề | Biểu hiện |
|--------|-----------|
| STATUS className | `bg-emerald-50 text-emerald-700 border-emerald-200`, `bg-amber-50`… — cần ink-based tones |
| Form fields | Minor: `text-gray-700` labels |

---

## 2. Design tokens (kế thừa — không đổi)

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
| Danger | `#b91c1c` |

**Quy tắc bổ sung cho trang tool (organizer):**
- Stat/counter icons: `background: rgba(15,15,15,0.04)`, icon color tùy ngữ cảnh (xanh/vàng/đỏ được phép dùng nhẹ)
- Colored alert banners (warning, error): giữ màu nhưng làm nhẹ — dùng `rgba(245,158,11,0.08)` thay `bg-amber-50`, border `rgba(245,158,11,0.20)` thay `border-amber-200`
- Table header: `background: rgba(15,15,15,0.03)`, `color: rgba(15,15,15,0.55)`, `fontSize: 11px, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em'`

---

## 3. Đợt A — MyEvents

### 3.1 Vấn đề ưu tiên
- Filter cards 6 ô màu sắc → clean stat bar + filter chips
- Event row: thumbnail nhỏ, thiếu hierarchy
- Empty state tự chế

### 3.2 Layout mới

```
┌─────────────────────────────────── max-w-5xl ─────────────────────────────────┐
│                                                                                 │
│  Sự kiện của tôi          [+ Tạo sự kiện]   ← h1 text-[22px] + btn-primary   │
│  {n} sự kiện tổng cộng                       ← caption ink-secondary          │
│                                                                                 │
│  ┌──────────────────────────────────────────────────────────────────────────┐  │
│  │ Tất cả 8  ·  Chờ duyệt 2  ·  Đang mở 3  ·  HT 2  ·  Từ chối 1  ...   │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│  ← Filter chips ngang (pill, #0F0F0F active, rgba(15,15,15,0.06) inactive)    │
│                                                                                 │
│  ┌──────── Event row (card) ──────────────────────────────────────────────┐   │
│  │ [img 72×72]  Title                              [Quản lý] [Sửa] [🗑] │   │
│  │              📅 ngày  📍 địa điểm  👥 0/50                             │   │
│  │              ⚠ Sự kiện đã kết thúc. Hãy hoàn thành hoặc hủy.          │   │
│  └────────────────────────────────────────────────────────────────────────┘   │
│  ┌──────── Event row ─────────────────────────────────────────────────────┐   │
│  │ ...                                                                     │   │
│  └────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 3.3 Chi tiết thay đổi

**Filter chips** (thay 6 colored cards):
```jsx
// Xóa: filterCards array với bg-yellow-50, border-yellow-200...
// Thêm: 1 dòng filter chips
<div style={{ display: 'flex', gap: 4, flexWrap: 'wrap', marginBottom: 20 }}>
  {filterCards.map(item => {
    const isActive = filter === item.key;
    return (
      <button key={item.key} onClick={() => setFilter(item.key)} style={{
        padding: '5px 14px', borderRadius: 999, border: 'none',
        fontSize: 13, fontWeight: isActive ? 600 : 400,
        background: isActive ? '#0F0F0F' : 'rgba(15,15,15,0.05)',
        color: isActive ? '#fff' : 'rgba(15,15,15,0.60)',
        cursor: 'pointer', transition: 'all 0.12s',
        display: 'flex', alignItems: 'center', gap: 6,
      }}>
        {item.label}
        <span style={{
          fontSize: 10, fontWeight: 700,
          background: isActive ? 'rgba(255,255,255,0.20)' : 'rgba(15,15,15,0.10)',
          color: isActive ? '#fff' : 'rgba(15,15,15,0.55)',
          borderRadius: 8, padding: '1px 6px',
        }}>
          {item.count}
        </span>
      </button>
    );
  })}
</div>
```

**Event row** — tăng thumbnail lên 80×80, cải thiện hierarchy:
```jsx
<div style={{
  display: 'flex', alignItems: 'center', gap: 16, padding: '16px 20px',
  background: '#fff', borderRadius: 12,
  border: '1px solid rgba(15,15,15,0.08)',
  transition: 'border-color 0.12s',
}}>
  {/* Thumbnail 80×80 */}
  <div style={{ width: 80, height: 80, borderRadius: 10, flexShrink: 0, overflow: 'hidden',
    background: 'rgba(27,97,201,0.06)', border: '1px solid rgba(15,15,15,0.07)' }}>
    {event.imageUrl ? <img ... /> : <i className="fa-solid fa-calendar-days" ... />}
  </div>

  {/* Content */}
  <div style={{ flex: 1, minWidth: 0 }}>
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4, flexWrap: 'wrap' }}>
      <span style={{ fontSize: 14, fontWeight: 600, color: '#0F0F0F' }}>{event.title}</span>
      <StatusBadge status={event.status} />
      {/* "Đang diễn ra" chip nếu cần */}
    </div>
    <div style={{ display: 'flex', gap: 12, fontSize: 12, color: 'rgba(15,15,15,0.50)', flexWrap: 'wrap' }}>
      <span><i className="fa-solid fa-calendar mr-1" />{fmt(event.startDate)}</span>
      {event.location && <span><i className="fa-solid fa-location-dot mr-1" />{event.location}</span>}
      <span><i className="fa-solid fa-users mr-1" />{event.currentParticipants}/{event.maxParticipants}</span>
    </div>
    {/* Warning: sự kiện đã kết thúc */}
    {event.status === 'Approved' && new Date(event.endDate) <= new Date() && (
      <p style={{ fontSize: 12, color: '#b45309', marginTop: 4 }}>
        <i className="fa-solid fa-triangle-exclamation mr-1" />
        Sự kiện đã kết thúc — hãy hoàn thành hoặc hủy.
      </p>
    )}
  </div>

  {/* Actions */}
  <div style={{ display: 'flex', gap: 6, flexShrink: 0, flexWrap: 'wrap' }}>
    {/* btn-secondary btn-sm, btn-primary btn-sm, btn-danger btn-sm — giữ nguyên classes */}
  </div>
</div>
```

**Empty state** — dùng component:
```jsx
// Xóa: <div className="card p-12 text-center"><i .../>...</div>
<EmptyState
  icon="fa-calendar-xmark"
  title="Chưa có sự kiện nào"
  description={filter === 'all' ? 'Tạo sự kiện đầu tiên để bắt đầu.' : `Không có sự kiện nào ở trạng thái "${filterCards.find(f => f.key === filter)?.label}".`}
  cta="Tạo sự kiện"
  ctaTo="/events/create"
/>
```

### 3.4 Files thay đổi — Đợt A
| File | Thay đổi |
|------|----------|
| `MyEvents.jsx` | Filter chips thay cards, event row polish, EmptyState, ink colors |

### 3.5 Acceptance criteria — Đợt A
- [ ] Không còn colored filter cards (`bg-yellow-50`, `bg-green-50`, etc.)
- [ ] Filter chips ngang, gọn, ink system
- [ ] Event row: thumbnail 80px, hierarchy title > meta
- [ ] Empty state dùng `EmptyState` component
- [ ] Build clean

---

## 4. Đợt B — ManageEvent/index.jsx (header + summary)

### 4.1 Vấn đề ưu tiên
- Summary cards màu → ink-system stat row
- Progress/capacity block xanh → clean card
- Header thiếu thumbnail sự kiện
- Tab bar thiếu nhất quán

### 4.2 Layout mới — Header

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  ← Quay lại  [img 56×56]  Tên sự kiện              [Kênh] [Chia ca]         │
│               📅 ngày · 📍 địa điểm    [Hoàn thành] [Hủy sự kiện]  [badge] │
└──────────────────────────────────────────────────────────────────────────────┘
```

```jsx
{/* Header card */}
<div style={{
  background: '#fff', borderRadius: 12, padding: '16px 20px',
  border: '1px solid rgba(15,15,15,0.08)',
  display: 'flex', alignItems: 'center', gap: 14, flexWrap: 'wrap',
}}>
  {/* Back btn */}
  <button onClick={() => navigate('/my-events')} style={{
    width: 32, height: 32, borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center',
    background: 'transparent', border: '1px solid rgba(15,15,15,0.10)', cursor: 'pointer',
    color: 'rgba(15,15,15,0.55)', flexShrink: 0,
  }}>
    <i className="fa-solid fa-arrow-left" style={{ fontSize: 13 }} />
  </button>

  {/* Thumbnail 56×56 */}
  <div style={{ width: 56, height: 56, borderRadius: 10, overflow: 'hidden', flexShrink: 0,
    background: 'rgba(27,97,201,0.06)', border: '1px solid rgba(15,15,15,0.07)' }}>
    {event.imageUrl ? <img src={event.imageUrl} className="w-full h-full object-cover" /> : null}
  </div>

  {/* Title + meta */}
  <div style={{ flex: 1, minWidth: 0 }}>
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
      <h1 style={{ fontSize: 16, fontWeight: 600, color: '#0F0F0F', margin: 0 }}>{event.title}</h1>
      <StatusBadge status={event.status} />
    </div>
    <p style={{ fontSize: 12, color: 'rgba(15,15,15,0.50)', marginTop: 2 }}>
      {fmt(event.startDate)} · {event.location}
    </p>
  </div>

  {/* Action buttons — giữ nguyên logic, chỉ đổi thứ tự visual */}
  <div style={{ display: 'flex', gap: 6, flexShrink: 0, flexWrap: 'wrap' }}>
    {event.channel?.id && <Link to={...} className="btn-secondary btn-sm">...</Link>}
    {canEnableShifts && <button className="btn-secondary btn-sm">...</button>}
    {event.status === 'Approved' && <button className="btn-primary btn-sm">Hoàn thành</button>}
    {canCancel && <button className="btn-danger btn-sm">Hủy sự kiện</button>}
  </div>
</div>
```

### 4.3 Layout mới — Summary stat row

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  2          │  5          │  3          │  75%        │  60%                 │
│  Chờ xác nhận  Đã xác nhận  Đã điểm danh  Fill rate    Tỉ lệ tham gia        │
└──────────────────────────────────────────────────────────────────────────────┘
```

```jsx
// Xóa: 3 colored cards bg-yellow-50/bg-green-50/bg-primary-50
// Thêm: 5-stat row (border-separated, không card)
<div style={{
  background: '#fff', borderRadius: 12, border: '1px solid rgba(15,15,15,0.08)',
  display: 'flex', flexWrap: 'wrap',
}}>
  {[
    { label: 'Chờ xác nhận', value: pending.length, color: '#b45309' },
    { label: 'Đã xác nhận',  value: confirmed.length, color: '#15803d' },
    { label: 'Đã điểm danh', value: attended.length, color: '#1b61c9' },
    { label: 'Lấp đầy', value: `${fillRate}%`, color: fillRate >= 100 ? '#b91c1c' : '#0F0F0F' },
    { label: 'Tham gia', value: `${attendanceRate}%`, color: '#0F0F0F' },
  ].map((s, i, arr) => (
    <div key={s.label} style={{
      flex: '1 1 100px', padding: '14px 20px', textAlign: 'center',
      borderRight: i < arr.length - 1 ? '1px solid rgba(15,15,15,0.07)' : 'none',
    }}>
      <p style={{ fontSize: 22, fontWeight: 700, color: s.color, lineHeight: 1.1 }}>{s.value}</p>
      <p style={{ fontSize: 11, color: 'rgba(15,15,15,0.45)', marginTop: 3 }}>{s.label}</p>
    </div>
  ))}
</div>
```

### 4.4 Capacity progress block

```jsx
// Xóa: <div className="rounded-lg border border-blue-100 bg-blue-50 p-4">
// Thay: clean card không màu nền
<div style={{
  background: '#fff', borderRadius: 12, padding: '16px 20px',
  border: '1px solid rgba(15,15,15,0.08)',
}}>
  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      <div style={{ width: 32, height: 32, borderRadius: 8, background: 'rgba(27,97,201,0.08)',
        display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <i className="fa-solid fa-users" style={{ color: '#1b61c9', fontSize: 13 }} />
      </div>
      <div>
        <p style={{ fontSize: 13, fontWeight: 600, color: '#0F0F0F' }}>
          {activeRegistrations.length} / {capacity} người đăng ký
        </p>
        <p style={{ fontSize: 11, color: 'rgba(15,15,15,0.45)' }}>
          {pending.length > 0 ? `${pending.length} chờ xác nhận · ` : ''}{confirmed.length} đã xác nhận
        </p>
      </div>
    </div>
    <span style={{ fontSize: 13, fontWeight: 600, color: fillRate >= 100 ? '#b91c1c' : '#1b61c9' }}>
      {fillRate}%
    </span>
  </div>
  {/* Progress bar */}
  <div style={{ height: 3, borderRadius: 99, background: 'rgba(15,15,15,0.07)' }}>
    <div style={{ width: `${Math.min(fillRate, 100)}%`, height: '100%', borderRadius: 99,
      background: fillRate >= 100 ? '#b91c1c' : '#1b61c9', transition: 'width 0.3s' }} />
  </div>
</div>
```

### 4.5 Tab bar

Dùng hệ thống tab tương tự volunteer pages — underline style:

```jsx
// Tab bar
<div style={{ borderBottom: '1px solid rgba(15,15,15,0.08)', background: '#fff',
  borderRadius: '12px 12px 0 0', display: 'flex', overflowX: 'auto', scrollbarWidth: 'none' }}>
  {manageTabs.map(t => (
    <button key={t.key} onClick={() => setTab(t.key)} style={{
      flexShrink: 0, padding: '11px 16px', border: 'none', background: 'none', cursor: 'pointer',
      display: 'flex', alignItems: 'center', gap: 6,
      fontSize: 13, fontWeight: tab === t.key ? 600 : 400,
      color: tab === t.key ? '#0F0F0F' : 'rgba(15,15,15,0.50)',
      borderBottom: tab === t.key ? '2px solid #0F0F0F' : '2px solid transparent',
      marginBottom: -1, transition: 'color 0.12s',
    }}>
      <i className={`fa-solid ${t.icon}`} style={{ fontSize: 11 }} />
      {t.label}
    </button>
  ))}
</div>
```

### 4.6 Files thay đổi — Đợt B
| File | Thay đổi |
|------|----------|
| `ManageEvent/index.jsx` | Header thumbnail, stat row, capacity card, tab bar — logic không đổi |

---

## 5. Đợt C — ManageEvent tabs

### 5.1 Nguyên tắc chung cho tất cả tabs
- **Alert banners**: `bg-amber-50 border-amber-200` → `rgba(245,158,11,0.08)` + `rgba(245,158,11,0.20)`
- **Empty states**: inline div+icon → `EmptyState` component
- **Table header**: `table-header` class → inline style ink (`rgba(15,15,15,0.03)` bg, uppercase 11px, letterSpacing)
- **Colored status pills** (trong tab): giữ màu ý nghĩa nhưng làm nhẹ (dùng rgba)
- **Section dividers**: `hr` / border → `1px solid rgba(15,15,15,0.07)`

### 5.2 RegistrationsTab (247 dòng)

| Thay đổi | Chi tiết |
|----------|----------|
| Warning banner | `bg-amber-50 border-amber-200` → rgba amber tones |
| Empty state | `div.card p-12` → `EmptyState` icon `fa-users` |
| Table header | `table-header` class → inline style ink |
| Action buttons | Giữ nguyên btn classes, chỉ đảm bảo spacing nhất quán |
| Pagination | Giữ nguyên logic, thêm margin/style nhất quán |

### 5.3 ShiftsTab (140 dòng)

| Thay đổi | Chi tiết |
|----------|----------|
| Shift cards | Đổi màu inline styles sang ink system |
| Empty state | `EmptyState` icon `fa-clock` |
| "Ca đầy" indicator | `bg-red-50 text-red-600` → `rgba(185,28,28,0.08)` + `#b91c1c` |

### 5.4 CheckInTab (226 dòng)

| Thay đổi | Chi tiết |
|----------|----------|
| QR code panel | Đảm bảo clean background, không hardcoded colors |
| Success/error messages | `bg-green-50` / `bg-red-50` → rgba tones |
| Input scan field | `input-field` class, nhất quán |

### 5.5 CampaignsTab (288 dòng)

| Thay đổi | Chi tiết |
|----------|----------|
| Status pills | Tailwind color classes → ink-based rgba tones |
| Campaign card | Clean border card, không background màu |
| Progress bar | Giữ màu nhưng height/borderRadius nhất quán |
| Empty state | `EmptyState` icon `fa-hand-holding-heart` |

### 5.6 CorporateTab (269 dòng)

| Thay đổi | Chi tiết |
|----------|----------|
| Proposal status pills | Tailwind colors → rgba ink |
| Empty state | `EmptyState` icon `fa-handshake` |
| Received amount block | `bg-green-50 border-green-200` → rgba green |

### 5.7 ReportTab (139 dòng)

Nhỏ nhất, ít thay đổi:
- Form section header → SectionLabel
- Alert banner → rgba amber

### 5.8 Files thay đổi — Đợt C
| File | Thay đổi |
|------|----------|
| `RegistrationsTab.jsx` | Table ink, warning banner, EmptyState |
| `ShiftsTab.jsx` | Card ink, EmptyState |
| `CheckInTab.jsx` | Message ink, cleanup |
| `CampaignsTab.jsx` | Status pills ink, EmptyState |
| `CorporateTab.jsx` | Status pills ink, EmptyState |
| `ReportTab.jsx` | SectionLabel, banner ink |

---

## 6. Đợt D — OrganizerInsights

### 6.1 StatCard (thay gradient)

```
Old StatCard (gradient):
┌─────────────────────────────┐
│ Số lượng TNV         [████] │
│ 1.247                [icon] │  ← gradient circle
│ +12 tuần này                │
└─────────────────────────────┘

New StatCard (ink):
┌─────────────────────────────┐
│ [icon]  Số lượng TNV        │  ← subtle icon, no gradient
│         1.247               │
│         +12 tuần này        │
└─────────────────────────────┘
```

```jsx
// Xóa: gradient div + separate icon box
// Thay:
function StatCard({ icon, label, value, sub, accentColor = '#1b61c9' }) {
  return (
    <div style={{
      background: '#fff', borderRadius: 12, padding: '18px 20px',
      border: '1px solid rgba(15,15,15,0.08)',
    }}>
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 12 }}>
        <div style={{
          width: 36, height: 36, borderRadius: 10, flexShrink: 0,
          background: `${accentColor}12`,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
        }}>
          <i className={`fa-solid ${icon}`} style={{ color: accentColor, fontSize: 14 }} />
        </div>
        <div>
          <p style={{ fontSize: 12, color: 'rgba(15,15,15,0.50)', marginBottom: 4 }}>{label}</p>
          <p style={{ fontSize: 24, fontWeight: 700, color: '#0F0F0F', lineHeight: 1.1 }}>{value}</p>
          {sub && <p style={{ fontSize: 11, color: 'rgba(15,15,15,0.40)', marginTop: 3 }}>{sub}</p>}
        </div>
      </div>
    </div>
  );
}
```

Accent colors tương ứng với old tones:
- `blue` tone → `#1b61c9`
- `green` tone → `#15803d`
- `amber` tone → `#b45309`
- `violet` tone → `#7c3aed`

### 6.2 ProgressRow (slate → ink)

```jsx
// Xóa: text-slate-700, text-slate-950, bg-slate-100
function ProgressRow({ label, value, max, color = '#1b61c9' }) {
  const percent = max > 0 ? Math.min(100, Math.round((value * 100) / max)) : 0;
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, fontSize: 13 }}>
        <span style={{ fontWeight: 500, color: '#0F0F0F' }}>{label}</span>
        <span style={{ fontWeight: 600, color: '#0F0F0F' }}>{formatNumber(value)}</span>
      </div>
      <div style={{ height: 6, borderRadius: 99, background: 'rgba(15,15,15,0.07)', overflow: 'hidden' }}>
        <div style={{ width: `${percent}%`, height: '100%', background: color, borderRadius: 99, transition: 'width 0.4s' }} />
      </div>
    </div>
  );
}
```

### 6.3 EmptyPanel → EmptyState component

```jsx
// Xóa: EmptyPanel local component
// Dùng: <EmptyState icon="fa-chart-simple" title={title} description={message} />
```

### 6.4 Filter bar — ink system
- `ring-1 ring-slate-200` trên cards → `1px solid rgba(15,15,15,0.08)`
- `bg-slate-100` filter inputs background → `rgba(15,15,15,0.03)`

### 6.5 Files thay đổi — Đợt D
| File | Thay đổi |
|------|----------|
| `OrganizerInsights.jsx` | StatCard ink, ProgressRow ink, EmptyPanel → EmptyState, filter bar ink |

---

## 7. Đợt E — EventForm

### 7.1 Multi-step indicator

```
Old: tự chế inline style

New:
┌──────────────────────────────────────────────────────────────┐
│  ① Thông tin  ━━━━  ② Thời gian  ━━━━  ③ Hình ảnh  ━━━━  ④ Xác nhận │
│     [active]            [done✓]                                     │
└──────────────────────────────────────────────────────────────┘
```

Dùng inline style nhất quán với ink system, không dùng Tailwind màu:
```jsx
// Mỗi step: 
{
  active:   background '#0F0F0F', color '#fff', 
  done:     background 'rgba(15,15,15,0.07)', color 'rgba(15,15,15,0.55)',
  locked:   background 'rgba(15,15,15,0.03)', color 'rgba(15,15,15,0.30)'
}
// Connector line: 1px solid rgba(15,15,15,0.10) → 1px solid rgba(15,15,15,0.25) if done
```

### 7.2 Section headers

```jsx
// Xóa: <h2 className="text-lg font-semibold text-gray-900">Thông tin cơ bản</h2>
//      <p className="mt-1 text-sm text-gray-500">...</p>
// Thay:
<SectionLabel>Thông tin cơ bản</SectionLabel>
<p style={{ fontSize: 13, color: 'rgba(15,15,15,0.50)', marginTop: -8, marginBottom: 16 }}>
  Đặt tên ngắn gọn, mô tả rõ mục tiêu...
</p>
```

### 7.3 "Cần xác minh" block

```jsx
// Xóa: bg-amber-50 text-amber-600 icon box
// Thay: clean card với border ink
<div style={{ background: '#fff', borderRadius: 12, padding: '24px', border: '1px solid rgba(15,15,15,0.08)' }}>
  <div style={{ width: 40, height: 40, borderRadius: 10, background: 'rgba(180,83,9,0.08)',
    display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: 12 }}>
    <i className="fa-solid fa-building-shield" style={{ color: '#b45309', fontSize: 18 }} />
  </div>
  <h1 style={{ fontSize: 18, fontWeight: 600, color: '#0F0F0F', margin: '0 0 8px' }}>
    Cần xác minh tổ chức trước khi tạo sự kiện
  </h1>
  ...
</div>
```

### 7.4 Labels + input meta text

```jsx
// Xóa: className="block text-sm font-medium text-gray-700"
// Thay: style={{ fontSize: 13, fontWeight: 500, color: 'rgba(15,15,15,0.70)', marginBottom: 4, display: 'block' }}

// Xóa: className="text-xs text-gray-500" (helper text)
// Thay: style={{ fontSize: 12, color: 'rgba(15,15,15,0.45)', marginTop: 4 }}
```

### 7.5 Files thay đổi — Đợt E
| File | Thay đổi |
|------|----------|
| `EventForm.jsx` | Step indicator ink, SectionLabel, label styles, verification block ink |

---

## 8. Đợt F — OrganizerVerification

### 8.1 Status badge

```jsx
// Thay Tailwind color classes bằng inline style map:
const STATUS_STYLE = {
  Unverified:            { color: 'rgba(15,15,15,0.55)',  bg: 'rgba(15,15,15,0.05)',  border: 'rgba(15,15,15,0.12)' },
  PendingVerification:   { color: '#b45309',               bg: 'rgba(180,83,9,0.08)',  border: 'rgba(180,83,9,0.20)' },
  ChangesRequested:      { color: '#b45309',               bg: 'rgba(180,83,9,0.08)',  border: 'rgba(180,83,9,0.20)' },
  Rejected:              { color: '#b91c1c',               bg: 'rgba(185,28,28,0.07)', border: 'rgba(185,28,28,0.18)' },
  Verified:              { color: '#15803d',               bg: 'rgba(21,128,61,0.08)', border: 'rgba(21,128,61,0.20)' },
};
```

### 8.2 Form cleanup
- Labels: `text-sm font-medium text-gray-700` → ink style
- Description/helper text: `text-gray-500` → `rgba(15,15,15,0.45)`
- Admin note alert: `bg-amber-50 border-amber-200` → rgba amber

### 8.3 Files thay đổi — Đợt F
| File | Thay đổi |
|------|----------|
| `OrganizerVerification.jsx` | STATUS_STYLE map, label styles |

---

## 9. Sequencing và ước tính

| Đợt | Files | Dòng thay đổi | Rủi ro | Ưu tiên |
|-----|-------|---------------|--------|---------|
| **A** | MyEvents.jsx | ~80 | Thấp — 1 file đơn | ★★★ Cao nhất |
| **B** | ManageEvent/index.jsx | ~120 (header/stats/tabs) | Trung bình — 1103 dòng, cẩn thận không chạm logic | ★★★ |
| **C** | 6 tab files | ~200 total | Thấp — tabs biệt lập | ★★ |
| **D** | OrganizerInsights.jsx | ~60 | Thấp — chủ yếu là UI | ★★ |
| **E** | EventForm.jsx | ~150 | Cao — 993 dòng, multi-step form | ★★ |
| **F** | OrganizerVerification.jsx | ~40 | Thấp | ★ Thấp nhất |

**Thứ tự đề xuất:** A → D → F → B → C → E

Lý do: A+D+F là quick wins, không rủi ro. B+C là ManageEvent core — làm cùng nhau vì tightly coupled. E (EventForm) để cuối vì phức tạp nhất.

---

## 10. Components tái sử dụng (không tạo mới)

| Component | Dùng ở đâu |
|-----------|-----------|
| `SectionLabel` | EventForm (section headers), ReportTab, ManageEvent |
| `EmptyState` | MyEvents, RegistrationsTab, ShiftsTab, CampaignsTab, CorporateTab |
| `StatusBadge` | Đã dùng, giữ nguyên |
| `Tabs` | *(Optional)* ManageEvent tab bar nếu refactor thêm |
| `Pagination` | RegistrationsTab đã có, giữ nguyên |

---

## 11. Trạng thái

- **2026-05-27** — Kế hoạch lập xong.
- **2026-05-27** — Triển khai toàn bộ 6 đợt theo thứ tự A → D → F → B → C → E. Build clean.
- **Đợt A (MyEvents):** ✅ Filter chips ink thay 6 colored cards; event row thumbnail 80px + hierarchy; EmptyState component; header có subtitle đếm tổng.
- **Đợt B (ManageEvent header/stats):** ✅ Header card có thumbnail 56px + back button; 5-stat row (Chờ/Xác nhận/Điểm danh/Lấp đầy/Tham gia); capacity card với progress bar ink; tab bar underline ink.
- **Đợt C (ManageEvent tabs):** ✅ RegistrationsTab (warning banner rgba, EmptyState, table header ink); ShiftsTab (EmptyState); CheckInTab (success/error message rgba); CampaignsTab (EmptyState, inner tiles rgba); CorporateTab (EmptyState, PROPOSAL_STATUS_STYLE pills ink). ReportTab giữ nguyên (đã sạch, dùng primary/semantic hợp lệ).
- **Đợt D (OrganizerInsights):** ✅ StatCard gradient → ink với accentColor; ProgressRow slate → ink; EmptyPanel → EmptyState; StatusPill ink (STATUS_STYLE); filter bar + table header + breakdown panels chuyển ink.
- **Đợt E (EventForm):** ✅ Notice → NOTICE_STYLE rgba; 6 section header → SectionHeading ink; step indicator (desktop + mobile) ink #0F0F0F active / #15803d done; verification gate block ink; skill chips ink; KYC checkbox ink; toàn bộ label → LABEL_STYLE.
- **Đợt F (OrganizerVerification):** ✅ STATUS map → rgba (color/bg/border); status badge inline; alert banners (adminNote/error/success/verified) rgba; toàn bộ label → LABEL_STYLE; commitment checkbox card ink; spinner ink.
