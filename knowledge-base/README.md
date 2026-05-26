# RMMS 2026 — Knowledge Base for AI Tools

Bộ knowledge base này được thiết kế để **bất kỳ AI coding tool nào** (Claude, ChatGPT, Cursor, Copilot, Windsurf, Cody...) đều có thể ingest và hiểu context dự án RMMS 2026 Phase 1.

## 🚀 BẮT ĐẦU TỪ ĐÂY (cho AI session mới và dev mới)

**ĐỌC FILE NÀY TRƯỚC TIÊN:** [`PROJECT-STATE.md`](./PROJECT-STATE.md)

Đây là snapshot trạng thái dự án **ngay lúc này** — đã build cái gì, chưa build cái gì, đang block ở đâu, bước kế tiếp là gì. Mọi file khác là spec (mong đợi), `PROJECT-STATE.md` là thực tế (đã làm).

Sau đó:
1. [`CHANGELOG.md`](./CHANGELOG.md) — log lịch sử đã làm gì khi nào
2. [`decisions/`](./decisions/) — ADRs đã chốt (kiến trúc, lib, pattern)
3. [`sprints/sprint-00.md`](./sprints/sprint-00.md) — sprint hiện tại + tasks còn lại
4. Các file spec dưới đây (theo nhu cầu)

## Cách sử dụng

### Option 1: Claude Projects
1. Tạo project mới trên Claude.ai
2. Upload toàn bộ thư mục này vào Project Knowledge
3. Trong instructions của project, paste nội dung file `prompts/system-prompt.md`
4. Mọi conversation trong project sẽ tự có context đầy đủ

### Option 2: Cursor IDE
1. Copy thư mục `knowledge-base/` vào root của repo
2. Tạo file `.cursor/rules/rmms.mdc` với nội dung trỏ tới các file knowledge
3. Hoặc dùng `@Docs` feature để index folder này

### Option 3: ChatGPT Custom GPT
1. Tạo Custom GPT mới
2. Upload file `system-prompt.md` làm instructions
3. Upload các file .md khác làm knowledge base
4. Bật Code Interpreter để GPT có thể đọc

### Option 4: GitHub Copilot Chat
1. Commit thư mục này vào repo
2. Mở conversation trong VSCode với Copilot Chat
3. Reference: `@workspace` hoặc paste content vào chat

## Cấu trúc

```
knowledge-base/
├── README.md                          # This file
├── PROJECT-STATE.md                   # 🚀 LIVE STATUS — read this first
├── CHANGELOG.md                       # Historical log of milestones
├── decisions/                         # Architecture Decision Records (ADRs)
│   └── README.md                      # ADR template + index
├── 00-overview.md                     # Project context, goals, stakeholders
├── 01-glossary.md                     # Terminology: PG, Leader, BUH, etc.
├── 02-tech-stack.md                   # Technology decisions
├── 03-architecture.md                 # System architecture
├── 04-data-model.md                   # Entities, relationships
├── 05-api-conventions.md              # REST conventions, auth, errors
├── 06-business-rules.md               # Decision tables from PRD section 3
├── 07-acceptance-criteria.md          # 35 acceptance criteria
├── 08-coding-standards.md             # Style guide BE/FE/Mobile
├── modules/                           # Detail per module
│   ├── M01-identity-access.md
│   ├── M02-device-management.md
│   ├── M03-organization-assignment.md
│   ├── M04-product-master.md
│   ├── M05-attendance-antifraud.md
│   ├── M06-face-verification.md
│   ├── M07-work-schedule.md
│   ├── M08-leave-ot.md
│   ├── M09-approval-workflow.md
│   ├── M10-form-engine.md
│   ├── M11-visit-plan.md
│   ├── M12-team-monitoring.md
│   ├── M13-document-center.md
│   ├── M14-news-notification.md
│   ├── M15-dashboard-reports.md
│   └── M16-admin-review-audit.md
├── sprints/                           # Sprint-by-sprint plan
│   ├── sprint-00-setup.md
│   ├── sprint-01.md ... sprint-18.md
└── prompts/                           # Ready-to-use prompts
    ├── system-prompt.md               # Master context for any AI
    ├── implementation-prompts.md      # Prompts to generate code per module
    ├── review-prompts.md              # Prompts for code review
    └── test-prompts.md                # Prompts for test generation
```

## Triết lý của knowledge base này

1. **AI-readable first**: Mỗi file là Markdown thuần, tránh formatting phức tạp
2. **Self-contained per file**: Mỗi module/sprint có đủ context để standalone
3. **Cross-referenced**: Dùng relative links giữa các file
4. **Source of truth**: PRD gốc là master; file này là derived view có cấu trúc
5. **Living document**: Update khi có decision mới, không phải snapshot

## Conventions

- **File names**: kebab-case, có prefix số để giữ thứ tự
- **Acceptance criteria refs**: `AC-1`, `AC-2`, ..., `AC-35`
- **Module refs**: `M1`, `M2`, ..., `M16`
- **Sprint refs**: `S0` (setup), `S1` ... `S18`

## Maintenance

Khi có thay đổi:

| Loại thay đổi | File cần update |
|---|---|
| Hoàn thành 1 capability lớn (module, integration, sprint) | `PROJECT-STATE.md` + `CHANGELOG.md` |
| Chốt quyết định kiến trúc / lib / pattern | Tạo ADR mới trong `decisions/` + update index trong `decisions/README.md` |
| Spec thay đổi (business rule, API contract…) | File spec tương ứng (`06-business-rules.md`, `05-api-conventions.md`, …) + ghi entry trong `CHANGELOG.md` |
| Sprint progress | `sprints/sprint-XX.md` (tick checkbox + ngày) |

**Quy tắc cho AI sessions:** Khi user yêu cầu làm xong việc gì có scope > 1 file code, **bắt buộc cập nhật `PROJECT-STATE.md` + `CHANGELOG.md` trước khi kết thúc session**, để session sau không phải đoán đã làm gì.

---

**Knowledge base initial commit**: 2026-05-22  
**Last structural update**: 2026-05-24 (added PROJECT-STATE.md + CHANGELOG.md + decisions/)  
**Plan version**: v1.0  
**Source PRD**: AppRMMS2026.pdf (Document v1.0)
