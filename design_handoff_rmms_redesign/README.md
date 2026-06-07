# Handoff: RMMS Mobile — UI/UX Refresh 2026

## Overview
This package documents a full visual + IA refresh of the **RMMS** field‑workforce attendance app (PG / merchandising staff). It modernizes the existing flat‑indigo Material 3 design into a younger, bolder system: vibrant indigo→violet **mesh gradients**, large display typography, rounded **bento** cards, a glass **bottom tab bar** with a center "Chấm công" action, and tasteful micro‑interactions.

It also **cleans up the information architecture** so no function appears in two menus (see *Navigation & IA* — this is a required behavioral change, not just visuals).

Target codebase: the **Flutter** app in `mobile/` (Material 3, `lib/core/theme/…`, `lib/features/…`). Implement by updating the theme tokens and rebuilding the feature screens — do **not** ship the HTML.

## About the Design Files
The files in this bundle (`RMMS Redesign.html` + `*.jsx`) are **design references built in HTML/React** — interactive prototypes that show the intended look, layout, copy, and behavior. They are **not** production code to copy.

The task is to **recreate these designs inside the existing Flutter app** using its established patterns: Material 3, the `AppPalette` / `AppTheme` / `AppSemantics` token system in `lib/core/theme/`, the per‑feature folder structure under `lib/features/`, and whatever state/routing the app already uses (`app_router.dart`). Match the prototype pixel‑for‑pixel where practical, expressed through Flutter widgets.

## Fidelity
**High‑fidelity.** Final colors, typography, spacing, radii, shadows, and interactions are specified below. Recreate faithfully using Flutter widgets and the codebase's theme system.

---

## Navigation & Information Architecture (IMPORTANT)
The refresh consolidates duplicated menus. Each function must live in exactly **one** place.

**Bottom tab bar (5 items, glass, center FAB):**
1. **Trang chủ** (Home)
2. **Lịch** (Work schedule list)
3. **Chấm công** (center, raised circular button → Attendance screen)
4. **Đơn từ** (Requests: leave / OT)
5. **Hồ sơ** (Profile / account)

**Home** shows today's shift status + shortcuts ONLY to screens that are *not* tabs: **Phân công**, **Lịch sử chấm công**, **Khuôn mặt**.

**Hồ sơ** is account‑only: **Thông báo**, **Đổi mật khẩu**, **Trợ giúp & hỗ trợ**, **Đăng xuất**.

Removed duplicates vs. the old app: "Lịch làm việc" and the "Đơn nghỉ/OT" banner are no longer on Home (they are tabs); Phân công / Lịch sử / Khuôn mặt are no longer in Hồ sơ (they live on Home).

Screen stack (pushed, with back button): **Chấm công**, **Đăng ký lịch**, **Xin nghỉ phép**, **Đăng ký OT**, **Phân công**, **Khuôn mặt**, **Lịch sử chấm công**. **Đăng nhập** is the unauthenticated entry.

---

## Design Tokens

These supersede the values in `lib/core/theme/app_palette.dart`. Keep the `AppPalette` + `AppSemantics` (ThemeExtension) structure; just update values and add the gradients.

### Brand
| Token | Hex | Use |
|---|---|---|
| indigo (primary) | `#5B5BF0` | primary brand |
| indigoDeep | `#4338CA` | gradient end / pressed |
| indigoBright | `#7C7CFF` | gradient start / secondary |
| violet | `#8B5CF6` | mesh accent |

### Semantic accents
| Token | Hex |
|---|---|
| emerald (success) | `#10B981` |
| emeraldSoft (container) | `#ECFDF5` |
| amber (warning/pending) | `#F59E0B` |
| rose (error/destructive) | `#F43F5E` |
| sky (info) | `#0EA5E9` |

### Neutrals (cool, slightly warm)
| Token | Hex | Use |
|---|---|---|
| ink | `#161528` | headings / primary text |
| body | `#45445C` | body text |
| muted | `#8A89A3` | secondary text |
| faint | `#B6B5CC` | tertiary / chevrons |
| line | `#ECECF3` | dividers / dashed borders |
| surface | `#FFFFFF` | cards |
| surface‑2 | `#F5F5FB` | input fill / chips / soft buttons |
| bg (scaffold) | `#F1F1F8` | app background |

### Status chip colors (bg / fg pairs)
- neutral `#F1F1F8` / `#6E6D87`
- indigo `#EEEEFF` / `#4338CA`
- emerald `#E7FBF2` / `#059669`
- amber `#FFF6E5` / `#B45309`
- rose `#FFEEF1` / `#E11D48`
- sky `#E6F5FE` / `#0284C7`

### IconTile soft backgrounds (bg / icon color)
- indigo `#EEEEFF` / `#5B5BF0` · violet `#F3ECFF` / `#8B5CF6` · emerald `#E7FBF2` / `#059669` · amber `#FFF4E0` / `#EA9009` · sky `#E4F4FE` / `#0EA5E9` · rose `#FFECF0` / `#F43F5E`

### Gradients
- **grad‑brand** (buttons, FAB, avatars): `linear-gradient(135°, #7C7CFF 0%, #5B5BF0 48%, #4338CA 100%)`
- **grad‑mesh** (hero/header blocks): layered →
  `radial-gradient(120% 120% at 0% 0%, #8B5CF6 0%, transparent 55%)`,
  `radial-gradient(120% 120% at 100% 30%, #5B5BF0 0%, transparent 60%)`,
  `linear-gradient(140°, #4338CA, #5B5BF0)`.
  In Flutter approximate with a `Stack` of `Container`s using `RadialGradient` over a base `LinearGradient`, or a single `LinearGradient(#7C7CFF→#5B5BF0→#4338CA)` if simplifying.
- **grad‑emerald** (checked‑in state): `linear-gradient(135°, #34D399, #059669)`

### Spacing scale
4 · 8 · 10 · 12 · 14 · 16 · 18 · 22 · 24 (px). Screen horizontal padding = **16**. Card inner padding = **16–18**.

### Radius
| Name | px | Use |
|---|---|---|
| sm | 12–14 | chips, small buttons, inputs |
| md | 17 | primary buttons |
| lg | 20–22 | list rows, quick cards |
| xl | 26–30 | hero / feature cards |
| full | 999 | chips, FAB ring, pills |

### Shadows
- **shadow‑sm**: `0 1px 2px rgba(22,21,40,.04), 0 4px 12px rgba(22,21,40,.04)`
- **shadow**: `0 2px 6px rgba(22,21,40,.05), 0 14px 36px rgba(22,21,40,.07)`
- **shadow‑lg**: `0 8px 24px rgba(67,56,202,.16), 0 24px 56px rgba(67,56,202,.14)`
- **shadow‑brand** (on gradient buttons): `0 10px 30px rgba(91,91,240,.38)`

### Typography
- **Display** (page titles, big numbers, clock): `Space Grotesk`, weight 700, negative tracking (~‑0.6 to ‑1.0).
- **Body / UI**: `Plus Jakarta Sans`, weights 400/500/600/700/800.
- Add both via `google_fonts` (or bundled assets). Replace any Inter/Roboto usage.

**Type scale (size / weight):**
| Role | px | weight |
|---|---|---|
| Page title (display) | 25–26 | 800 |
| Big clock | 44 | 700 |
| Hero name | 22 | 800 |
| Card title | 15.5–16.5 | 800 |
| Row title | 15.5 | 700 |
| Body | 14.5–15.5 | 500–600 |
| Label / chip | 12.5–13 | 700 |
| Eyebrow (UPPERCASE) | 11.5–12.5 | 800, letter‑spacing ~1px |

---

## Shared Components
Build these as reusable Flutter widgets (mirror the prototype's `ds.jsx`).

- **StatusBar / device chrome**: provided by the OS — ignore the prototype's mock status bar.
- **TopBar**: 40×40 back button (rounded 13, white surface, `shadow‑sm`, chevron‑left), title 19/700, optional trailing actions.
- **Chip**: pill, height 28, padding 0/11, 12.5/700, optional 14px leading icon; tone bg/fg pairs above; `solid` variant fills with fg color + white text.
- **Card**: surface, radius 26–28 (`xl`), `shadow‑sm`, padding 16–18. Pressable variant scales to 0.965 on tap.
- **IconTile**: rounded square (size ~46–48, radius 15–16) with soft tinted bg + stroke icon at ~50% size.
- **Button**: height 54, radius 17, 16/700, optional 20px leading icon, `whiteSpace: nowrap`. Variants: `primary` (grad‑brand + shadow‑brand, white), `emerald` (grad‑emerald), `soft` (surface‑2, indigoDeep text), destructive‑soft (`#FFEEF1` bg, rose text).
- **Sheet**: bottom sheet, top radius 30, grab handle, slide‑up animation, scrim `rgba(20,19,40,.4)`.
- **Toast**: floating dark pill (`#161528`), white text, leading status icon, auto‑dismiss ~1.9s; rises from bottom.
- **Bottom nav (glass)**: height 70, margin 14, radius 26, translucent white + blur(20px) + subtle border + `shadow`. 4 labeled tab items (icon 24 + label 10.5/700; active = indigo, inactive = faint) and a **center raised FAB**: 58×58, radius 20, grad‑brand, `shadow‑brand`, 4px bg‑colored border, offset up −26.

**Icons**: stroke‑style, rounded caps/joins, ~2.1–2.5 weight (use the prototype's set as reference — map to `lucide`/`feather`/Material symbols in Flutter). Names used: home, calendar, check, checkCircle, clock, history, face (smiley), doc, store, users, user, logout, chevron L/R/down, plus, edit, send, undo, umbrella, otClock, bell, mail, lock, eye, pin, sparkle, arrowUpR.

**Micro‑interactions**
- Press feedback: scale 0.965, 140ms `cubic-bezier(.2,.8,.2,1)`.
- Entrance: subtle **transform‑only** rise (`translateY(16px)→0`, ~450ms, staggered 60ms). Do **not** animate opacity from 0 for critical content (keep base visible) and respect reduced‑motion.
- Attendance: pulsing ring around the check‑in avatar when not checked in; rotating dashed ring while scanning; live `HH:MM:SS` clock (tabular numerals).

---

## Screens / Views

> Copy text below is the exact Vietnamese used in the prototype.

### 1. Đăng nhập (Login) — entry
- Layout: top brand block (64×64 grad‑brand rounded‑22 logo tile with store icon), title **"Chào mừng\ntrở lại 👋"** (display, 34/800), subtitle "Đăng nhập để bắt đầu ca làm việc của bạn.".
- Fields: two pill inputs (height 58, radius 17, white, `shadow‑sm`): leading mail icon + email; leading lock icon + password with trailing eye toggle.
- "Quên mật khẩu?" right‑aligned link (indigo, 14/700).
- Primary button **"Đăng nhập"** (full width); shows spinner ~1.1s then routes to Home.
- Footer: "Chưa có tài khoản? **Đăng ký**".

### 2. Trang chủ (Home) — tab
- **Hero** (radius 30, grad‑mesh, shadow‑lg, two translucent decorative circles): 56×56 avatar tile "P"; "Chào buổi sáng 👋" + name "PG 01" (display 22/800, white); top‑right logout icon button; two glass chips "Vai trò · PG", "ST‑001".
- **Ca hôm nay card** (white, radius 26, shadow): left circular avatar (62, grad‑brand, smiley; pulsing ring when not checked in; switches to grad‑emerald + check when checked in). Eyebrow "CA HÔM NAY · 08:00–17:00"; title "Chưa chấm công" / "Đang trong ca"; live clock (Space Grotesk 21/700, indigoDeep). Trailing chevron tile. Taps → Chấm công.
- **Truy cập nhanh**: eyebrow "TRUY CẬP NHANH" + sparkle icon. 3‑column grid of square cards (radius 22, centered): **Phân công** (users, indigo), **Lịch sử** (history, emerald), **Khuôn mặt** (face, amber).

### 3. Lịch làm việc (Schedule) — tab
- Header row: page title "Lịch làm việc" (display 25/800, nowrap) + compact "**+ Đăng ký**" gradient button → Đăng ký lịch.
- **Summary row**: 3 stat cards — "3 / Ca tuần này" (indigo), "24h / Tổng giờ" (emerald), "1 / Chờ duyệt" (amber). Numbers in Space Grotesk.
- **Shift cards** (radius 28, left status accent bar 5px): date (14.5/800, nowrap) + status Chip; chips "08:00 – 17:00" (clock) and "ST‑001" (store). Action row (when not approved): **Sửa** (soft), then **Gửi duyệt** (gradient, for `draft`) or **Thu hồi** (rose‑soft, for `pending`). Status meta: pending=amber "Chờ duyệt", approved=emerald "Đã duyệt", draft=neutral "Nháp".

### 4. Đăng ký lịch (Register schedule) — pushed
- TopBar "Đăng ký lịch".
- Segmented control **Ngày / Tuần / Tháng** (track surface‑2, active = white pill + shadow). Selecting changes the "X ngày sẽ được tạo" hint (1/7/30).
- **Date card**: calendar IconTile + "Chọn ngày" / "CN, 7 thg 6, 2026" + emerald hint + chevron.
- **Ca làm** section: shift card(s) with a "Điểm bán ST‑001" dropdown row, and two time pickers **Bắt đầu 08:00** / **Kết thúc 17:00** (surface‑2, label + 22/800 time). Extra shifts get a "Xóa ca này" button. Dashed **"+ Thêm ca"** adds a shift.
- Sticky bottom **"Lưu lịch"** primary button (fades over bg).

### 5. Đơn từ (Requests) — tab
- Header: title "Đơn của tôi" (display 26/800, nowrap) + 46×46 gradient **+** button → opens create Sheet.
- **Segmented tabs**: "Nghỉ phép" (umbrella) / "Làm thêm (OT)" (otClock); active = ink fill + white, inactive = white + muted.
- **Request cards**: type Chip + status Chip; date (16/800); reason line; for `pending` a right‑aligned "Thu hồi" (rose‑soft) action. Footer "— Hết danh sách —".
- **Create Sheet** ("Tạo đơn mới"): two rows → "Xin nghỉ phép" (umbrella/indigo) → Leave, "Đăng ký OT" (otClock/amber) → OT.

### 6. Xin nghỉ phép / Đăng ký OT (Leave/OT) — pushed
- TopBar "Xin nghỉ phép" or "Đăng ký OT".
- Two date cards: "Từ ngày" / "Đến ngày" (calendar IconTile, indigo/violet) + chevron.
- OT variant adds an "Số giờ OT" card ("2.0 giờ", amber otClock).
- **Lý do** section: white textarea card (min‑height ~110), placeholder "Nhập lý do xin nghỉ…" / "Mô tả công việc làm thêm…", char counter "0/1000".
- Sticky bottom **"Gửi đơn"** → toast + back to Requests.

### 7. Phân công (Assignment) — pushed
- TopBar "Phân công của tôi".
- **LEADER QUẢN LÝ**: card with 52 grad‑brand initials tile "TL", "Team Lead 1" + "leader1@rmms.local", trailing emerald bell button; chip "0900 000 011".
- **ĐIỂM BÁN CỦA TÔI**: card with a grad‑mesh image header (store icon, 110 tall, diagonal stripe texture), then "Cửa hàng Quận 1" / "Mã ST‑001" + emerald chip "Đang hoạt động".

### 8. Chấm công (Attendance) — pushed (also center FAB)
- TopBar "Chấm công". Context chips "ST‑001 · Quận 1" (store), "Trong khu vực" (emerald pin).
- **Big face target** (230 area): soft gradient halo + dashed ring (rotates while scanning) + 150 circular gradient avatar with smiley (→ check + grad‑emerald when checked in).
- Big clock (Space Grotesk 44/700) + "CN, 7 tháng 6, 2026".
- **In/out** two cards: "VÀO CA" (emerald) / "RA CA" (rose), times or "--:--".
- Primary button: "Quét khuôn mặt & Check‑in" → spinner ~1.4s → toast; toggles to "Check‑out ngay" (emerald) when in a shift.

### 9. Khuôn mặt (Face) — pushed
- TopBar "Khuôn mặt". Large gradient face circle with an emerald check badge when registered.
- Title "Khuôn mặt đã đăng ký" / "Chưa có khuôn mặt" + helper text; chips "Đã xác thực", "Cập nhật 06/06".
- Buttons: "Đăng ký lại" / "Đăng ký khuôn mặt" (primary); "Xóa khuôn mặt" (rose‑soft) when registered.

### 10. Lịch sử chấm công (History) — pushed
- TopBar "Lịch sử chấm công".
- **Summary** grad‑mesh card: "Tháng 6 · giờ công 142h" and "Đúng giờ 96%".
- **Day rows**: IconTile (checkCircle=emerald on‑time / clock=amber late) + date + "07:58 → 17:03" + right‑aligned hours (Space Grotesk) + "Đúng giờ"/"Đi muộn" tag.

---

## Interactions & Behavior
- **Routing**: tabs swap the active screen; secondary items push onto a back stack (TopBar back pops). Center FAB pushes Attendance. Login replaces stack; logout returns to Login.
- **Check‑in/out**: shared `checkedIn` state drives Home card, Attendance, and FAB visuals. Action shows a brief loading spinner then a toast.
- **Schedule**: draft → "Gửi duyệt" sets pending; pending → "Thu hồi" sets draft. Toasts confirm.
- **Register**: segmented mode changes the day‑count hint; "Thêm ca"/"Xóa ca này" add/remove shift rows; "Lưu lịch" toasts + pops.
- **Requests**: tab switches leave/OT lists; "+" opens create sheet; "Thu hồi" toasts.
- **Forms**: reason textarea enforces 1000‑char max with live counter; password eye toggles visibility.
- **Animations**: press‑scale, staggered transform‑rise entrances, pulsing/rotating rings, live clock. Honor `prefers-reduced-motion` / Flutter's `MediaQuery.disableAnimations`.

## State Management
Use the codebase's existing approach. Minimum state:
- `authed` (login) · `route` + `activeTab` + back‑stack (navigation) · `checkedIn` (shift) · per‑screen lists: shifts (status enum: draft/pending/approved), requests (leave/OT with status), register draft (mode, date, shifts[]), face registered bool, leave/OT form (from, to, hours, reason).
Persist the current route locally for refresh‑resilience if helpful (the prototype uses localStorage).

## Assets
- **Fonts**: Space Grotesk (500/600/700), Plus Jakarta Sans (400–800) — add via `google_fonts` or bundle.
- **Icons**: stroke icon set (see list). Map to an existing Flutter icon library; no raster assets required.
- **Imagery**: store header uses a gradient + CSS stripe placeholder; replace with a real store photo slot if available. No other images.
- No proprietary/branded third‑party assets are used.

## Screenshots
Reference renders of every screen are in `screenshots/` (real‑browser captures, iPhone frame):
`01-login` · `02-home` · `03-schedule` · `04-register-schedule` · `05-requests` · `06-leave-request` · `07-assignment` · `08-attendance` · `09-face` · `10-history` · `11-profile`.

## Files (design references in this bundle)
- `RMMS Redesign.html` — entry; loads the others.
- `ds.jsx` — design‑system primitives (icons, Chip, Card, IconTile, Button, Sheet, Toast, TopBar).
- `screens-main.jsx` — Home, Attendance, Profile, Assignment.
- `screens-flows.jsx` — Schedule, Register, Leave/OT, Requests, Face, History, Login.
- `app-root.jsx` — router, bottom nav, state.
- `ios-frame.jsx` — device bezel (prototype‑only; ignore for implementation).

### Existing files to update in `mobile/`
- `lib/core/theme/app_palette.dart` — replace token values, add the brand/mesh/emerald gradients to `AppSemantics`.
- `lib/core/theme/app_theme.dart` — bump radii (button 17, cards 26–28), wire the new fonts, update shadows.
- `lib/features/*/presentation/` — rebuild each screen per above.
- `lib/core/widgets/brand_widgets.dart` — add the shared widgets (Chip, IconTile, Card, Button variants, glass BottomNav, Sheet, Toast).
