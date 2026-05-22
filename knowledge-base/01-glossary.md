# 01 — Glossary

Định nghĩa thuật ngữ dùng trong RMMS 2026. Khi nói chuyện với AI về dự án, dùng các thuật ngữ chính xác này.

## Roles & People

| Term | Definition |
|---|---|
| **PG** | Promotion Girl/Boy — nhân viên tiếp thị làm việc tại cửa hàng. Là role chính mà mobile app phục vụ. Đăng ký bằng email. |
| **Leader** | Quản lý trực tiếp của PG. Có tài khoản được Admin cấp. Quản lý 1 nhóm PG, có thể nhiều khu vực/store. Duyệt request của PG. |
| **Admin** | System administrator. Có tài khoản được cấp. Toàn quyền quản lý: users, stores, forms, products, override approvals, audit. |
| **BUH** | Business Unit Head — cấp cao hơn Leader. Có tài khoản được cấp. Duyệt request của Leader, duyệt Visit Plan, xem dashboard/reports. Có thể duyệt qua email link không cần login. |

## Org Entities

| Term | Definition |
|---|---|
| **Store** | Cửa hàng — nơi PG làm việc. Có tọa độ GPS để verify check-in. Store KHÔNG quyết định giờ làm/ca; chỉ là địa điểm. |
| **Area / Khu vực** | Nhóm các store theo địa lý. Dùng để gán PG/Leader và form assignment. |
| **Category / Ngành hàng** | Ngành hàng (FMCG, mỹ phẩm, điện máy...). Gán PG/Leader nếu cần cho form/product. |
| **Product** | Sản phẩm trong Product Master. Có category, brand, SKU. Là data source cho Form Engine product selector. |

## Operational Concepts

| Term | Definition |
|---|---|
| **Check-in / Check-out** | Chấm công bắt đầu/kết thúc ca làm. Bắt buộc tại store được phân công, có Face Recognition + selfie + photo cửa hàng + GPS. |
| **Shift / Ca làm** | Khoảng thời gian làm việc trong ngày. Định nghĩa per PG/Leader (KHÔNG per store). 1 ngày có thể nhiều ca. |
| **Work Schedule / Lịch làm việc** | Lịch đăng ký các shift theo ngày/tuần/tháng. Phải được duyệt mới có hiệu lực. |
| **Leave / Nghỉ phép** | Đơn xin nghỉ có kế hoạch. Có ngày bắt đầu/kết thúc, lý do. |
| **Emergency Leave / Nghỉ đột xuất** | Nghỉ trong lúc đang làm việc, gắn với hành động check-out giữa ca. |
| **OT / Overtime** | Đăng ký làm ngoài giờ. Có giờ bắt đầu/kết thúc, lý do. |
| **Visit Plan** | Kế hoạch viếng thăm cửa hàng của Leader. Có ngày, list stores, tasks/forms cần thực hiện. BUH duyệt. |
| **Visit Report** | Báo cáo sau khi Leader thực hiện visit. Dùng Form Engine. |

## Anti-Fraud / Security

| Term | Definition |
|---|---|
| **GPS Validation** | Kiểm tra vị trí thiết bị khi check-in/out. Threshold 300m từ tọa độ store. >300m = gửi Admin Review. |
| **Fake GPS** | App giả lập vị trí (Mock Location trên Android, location spoofing iOS). Nếu detect → block ngay, không cho check-in. |
| **Face Recognition** | Xác thực sinh trắc khuôn mặt khi check-in/out. Bắt buộc. Dùng 3rd-party API (FPT.AI). |
| **Face Enrollment** | Quá trình đăng ký khuôn mặt lần đầu của PG. Lưu template trong vendor system. |
| **Face Fail** | Khi face verification không match. Gửi Admin Review để Admin xem selfie xác nhận. |
| **Selfie** | Ảnh chụp người check-in tại thời điểm chấm công. Lưu để Admin review nếu cần. |
| **Single-device login** | PG chỉ được login trên 1 thiết bị. Đổi thiết bị → Leader/Admin duyệt. |
| **Device change request** | Quy trình PG yêu cầu đổi thiết bị mới, Leader hoặc Admin duyệt. |
| **Admin Review** | Hàng đợi các case bất thường (face fail, GPS violation, device change) chờ Admin xem xét. |

## Form Engine

| Term | Definition |
|---|---|
| **Form Engine** | Module cho phép Admin tự tạo form mà không cần dev. Trung tâm của module M10. |
| **Form Template** | Một loại form có sẵn (Stock Report, Market Report, Survey, Training, Test, ...). |
| **Form Builder** | UI để Admin tạo/sửa form: chọn fields, set rules. |
| **Form Assignment** | Gán form cho ai làm: by role/user/store/area/category/product, có thời gian. |
| **Form Rule** | Cấu hình behavior của form: required check-in? required GPS? scoring? randomize? offline draft?... |
| **Form Version** | Phiên bản của form. Khi Admin sửa form đã publish → tạo version mới. Submissions giữ reference version cũ. |
| **Offline Draft** | Lưu câu trả lời form khi mất mạng. Lưu trong Hive trên mobile. Submit lại khi có mạng. |
| **Form Submission** | Bản nộp form của user. Có thể edit-after-submit nếu form cấu hình cho phép. |
| **Product Selector / Store Selector / Brand-SKU Selector** | Các input types trong Form Engine, lấy data từ Product Master / Org entities. |

## Approval Workflow

| Term | Definition |
|---|---|
| **Request** | Đơn cần duyệt (Schedule registration, Schedule edit, OT, Leave, Emergency leave, Visit Plan). |
| **Approver** | Người duyệt. PG request → Leader. Leader request → BUH. |
| **Inline approval** | Duyệt ngay trên list mà không vào detail. Cho đơn đơn giản. |
| **Detail approval** | Vào màn chi tiết để xem đầy đủ trước khi duyệt. Cho đơn nhạy cảm. |
| **Reject reason** | Lý do từ chối — BẮT BUỘC nhập khi reject. |
| **BUH email-link approval** | BUH có thể duyệt qua link trong email mà không cần login. Link có signed token. |
| **Admin override** | Admin có thể override quyết định approval. Bắt buộc nhập lý do và ghi vào audit log. |
| **Audit Log** | Log toàn bộ thao tác quan trọng (login, check-in/out, approve/reject, override, form publish, export, etc.). |

## Document & Notification

| Term | Definition |
|---|---|
| **Document Center** | Nơi quản lý tài liệu public/private. Hỗ trợ PDF, image, text. |
| **Public Document** | Tài liệu mọi user xem được (theo role/assignment). |
| **Private Document** | Tài liệu chỉ user cụ thể xem được (ví dụ: bảng lương). |
| **Payslip** | Bảng lương. KHÔNG có module Salary trong Phase 1. Admin gửi dưới dạng file private. |
| **News** | Tin tức/thông báo do Admin tạo, gửi user. Phân loại, có read/unread. |
| **Important News** | Tin cần xác nhận đã đọc (user phải click confirm). |
| **In-app notification** | Thông báo trong app, có badge. |
| **Push notification** | Thông báo hệ thống điện thoại (FCM). |

## Status Enums

### Attendance Status (Phase 1)
- `Valid` — Hợp lệ
- `Late` — Trễ (>5 phút sau giờ vào ca)
- `GpsViolationPendingReview` — GPS > 300m, chờ Admin
- `FaceFailPendingReview` — Face không match, chờ Admin
- `FakeGpsBlocked` — Fake GPS detect, bị chặn
- `AdminApproved` — Sau khi Admin xác nhận đúng người
- `AdminRejected` — Sau khi Admin xác nhận sai người (không được chấm công)

### Schedule Status
- `Pending` — Chờ duyệt
- `Approved` — Đã duyệt
- `Rejected` — Từ chối
- `EditPending` — Đã sửa, chờ duyệt lại (lịch cũ vẫn còn hiệu lực)

### Visit Plan Status
- `Pending` — Chờ duyệt
- `Approved` — Đã duyệt
- `Rejected` — Từ chối
- `Executed` — Đã thực hiện

### Team Monitoring Status (real-time display)
- `Working` — Đang làm việc
- `NotCheckedIn` — Chưa check-in
- `CheckedOut` — Đã check-out
- `OnLeave` — Nghỉ phép
- `NoScheduleToday` — Không có lịch hôm nay
- `PendingReview` — Chờ Admin review (có bất thường)

## Acronyms

| Acronym | Meaning |
|---|---|
| PG | Promotion Girl/Boy |
| BUH | Business Unit Head |
| OT | Overtime |
| GPS | Global Positioning System |
| EXIF | Exchangeable Image File Format (metadata trong photo) |
| FCM | Firebase Cloud Messaging |
| BE | Backend |
| FE | Frontend |
| MVP | Minimum Viable Product |
| UAT | User Acceptance Testing |
| AC | Acceptance Criteria |
| DoD | Definition of Done |
| PR | Pull Request |
| SKU | Stock Keeping Unit |
