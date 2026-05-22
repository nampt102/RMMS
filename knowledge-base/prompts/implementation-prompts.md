# Implementation Prompts

Ready-to-use prompts for generating code, organized by task type. Copy any prompt into your AI tool of choice.

## How to use

1. Make sure system prompt (`system-prompt.md`) is loaded as context
2. Pick a prompt below matching your task
3. Fill in `{placeholders}`
4. Paste and run

---

## 1. Backend — Create a new module

```
I'm implementing module {M5 — Attendance & Anti-Fraud}.

Reference: `modules/M05-attendance-antifraud.md`

Please generate:
1. Domain entity (`Rmms.Domain/Entities/AttendanceRecord.cs`)
   - Match the schema in `04-data-model.md` table `attendance_records`
   - Include domain methods (e.g., `MarkLate()`, `RequireAdminReview(reason)`)
   - Use value objects for `GpsCoordinate` if appropriate
2. EF Core configuration (`Rmms.Infrastructure/Persistence/Configurations/AttendanceRecordConfiguration.cs`)
3. MediatR command for check-in (`Rmms.Application/Features/Attendance/CheckIn/CheckInCommand.cs`)
   - Command, Handler, Validator
4. Controller endpoint (`Rmms.Api/Controllers/AttendanceController.cs`)
   - Multipart upload for selfie + store photo
   - Idempotency support
5. Unit tests for the command handler
6. Integration test for the endpoint

Follow `08-coding-standards.md` for style. Use the patterns from `02-tech-stack.md`.
```

---

## 2. Backend — Add a new API endpoint to existing module

```
Add endpoint `POST /api/v1/{resource}/{action}` to module {M9 Approval Workflow}.

Behavior: {describe behavior in detail}

Implement:
1. Request DTO (record) with FluentValidation validator
2. Response DTO
3. MediatR command/query + handler
4. Controller method with proper attributes
5. Unit tests for the handler
6. Integration test for the endpoint
7. OpenAPI documentation comments

Use error codes from `05-api-conventions.md`. Add new codes if needed and propose them.
```

---

## 3. Backend — Generate EF Core migration

```
I need a migration that adds the following:

{describe DDL changes}

Generate:
1. The migration file (Up and Down methods)
2. Updated DbContext if new DbSet needed
3. Updated entity configuration
4. Seed data if applicable

Migration name should follow `YYYYMMDD_HHMM_short_description` format.
```

---

## 4. Mobile — Create a feature screen

```
Implement the {Check-in} screen for {PG} role.

Reference: 
- `modules/M05-attendance-antifraud.md` for behavior
- `08-coding-standards.md` section "Mobile" for conventions
- `02-tech-stack.md` for Flutter packages available

Generate (under `lib/features/{attendance}/`):
1. Riverpod provider for state management (`presentation/providers/check_in_provider.dart`)
2. The screen widget (`presentation/screens/check_in_screen.dart`)
3. Supporting widgets in `presentation/widgets/`
4. API method in `data/` using Retrofit (Dio)
5. Domain model with Freezed if not exists
6. Add i18n strings to `lib/l10n/app_vi.arb` and `app_en.arb`
7. Wire into go_router in `lib/core/router/app_router.dart`
8. Widget test for the screen

UI requirements:
- Camera preview
- GPS coords display with status indicator
- Auto-detect assigned store
- Submit button enabled only when face captured + GPS valid
- Show progress during face verification (~2s expected)
- Handle network errors gracefully
```

---

## 5. Web — Create an Admin screen

```
Implement the {User Management} screen at route `/[locale]/(dashboard)/users`.

Reference: `modules/M01-identity-access.md` and `modules/M03-organization-assignment.md`

Use:
- Ant Design Pro `ProTable` for the list
- `ProForm` for the create/edit modal
- TanStack Query for data fetching
- Zod schema for form validation
- next-intl for translations

Generate:
1. Page component (`app/[locale]/(dashboard)/users/page.tsx`)
2. Components in `components/features/users/`
3. API client functions in `lib/api/users.ts`
4. TypeScript types in `types/user.ts`
5. Translations added to `locales/vi.json` and `en.json`
6. Vitest test for the main component

Requirements:
- Filter by role, status
- Search by name/email
- Create new Leader/BUH/Admin (not PG — those self-register)
- Edit user details, toggle active/inactive
- Show last login, devices count
- Bulk operations: not in Phase 1
```

---

## 6. Generate from acceptance criterion

```
I'm implementing AC-{18}: "BUH can approve via email link WITHOUT login."

Reference:
- `06-business-rules.md` rule BR-407
- `modules/M09-approval-workflow.md` section "Email-link Approval"
- `05-api-conventions.md` for endpoint design

Please:
1. Design the data model for approval_email_tokens (already in `04-data-model.md`, confirm fields)
2. Implement the token generation service:
   - Generate HMAC-SHA256 signed token
   - Payload: { approval_id, approver_user_id, expires_at, nonce }
   - Store hashed in DB
3. Implement the public consume endpoint `GET /api/v1/approvals/email-action`
   - No JWT required
   - Validate token (exists, not expired, not used, signature valid)
   - Render simple HTML page with approve/reject form
4. Implement the confirm endpoint `POST /api/v1/approvals/email-action/confirm`
   - Apply decision
   - Mark token used
   - Log IP/UA
   - Audit log entry
5. Email template (bilingual) for sending the link
6. Tests:
   - Token validation edge cases
   - Token reuse rejection
   - Token expiry handling
   - Forged signature rejection

Output complete code following coding standards.
```

---

## 7. Bug fix prompt

```
There's a bug in {module M5 Attendance}:

Steps to reproduce:
1. {step}
2. {step}
3. {step}

Expected: {expected}
Actual: {actual}

Error logs: {paste logs}

Please:
1. Identify root cause (refer to relevant code paths)
2. Propose fix
3. Add a regression test that would catch this
4. Update business rules doc if behavior was undefined
```

---

## 8. Refactor prompt

```
Refactor {file path} to improve {testability/readability/performance}.

Constraints:
- Don't change public API
- Don't change DB schema
- Maintain backward compat
- All existing tests must still pass

Focus on:
- {specific concern}

Show before/after diff + explanation.
```

---

## 9. Database query optimization

```
This query is slow:

{paste query or LINQ expression}

Current behavior: {time, rows scanned}

Database: PostgreSQL 16
Table: {table_name}, ~{N} rows

Please:
1. Suggest index(es) to add
2. Suggest query rewrite if applicable
3. EXPLAIN ANALYZE expectation
4. Migration to add the index
```

---

## 10. New business rule

```
We need to add a new business rule:

{describe rule}

This affects modules: {list}

Please:
1. Write the rule in `06-business-rules.md` format
2. Identify affected code (paths)
3. Suggest implementation changes
4. List new test cases needed
5. Flag any conflicts with existing rules
```

---

## 11. End-of-sprint review

```
Sprint {N} is ending. Goal was: {goal}

Completed deliverables:
- {list}

Outstanding items:
- {list}

Please help me:
1. Identify which items must go to next sprint vs Phase 2 backlog
2. Update risk register if new risks emerged
3. Draft sprint retrospective discussion points
4. Update knowledge base files that need adjustment
```

---

## 12. Documentation generation

```
Generate user-facing documentation for module {M10 Form Engine}.

Audience: End users (Admin who will use Form Builder)
Format: Markdown with screenshots placeholders
Language: Vietnamese (primary) + English (secondary)

Sections:
- Overview
- Step-by-step: Create your first form
- Configuring form rules (with examples)
- Assigning forms
- Viewing results
- Versioning behavior
- Troubleshooting
- FAQ
```

---

## 13. API spec for a new feature

```
Design the API for {feature description}.

Reference `05-api-conventions.md` for our conventions.

Provide:
1. OpenAPI YAML spec
2. List of new error codes (with descriptions)
3. Suggested rate limits
4. Auth requirements
5. Sample request/response payloads
6. Migration impact (if any)
```

---

## 14. Testing prompt

```
Generate tests for {file or feature}.

Coverage requirements per `08-coding-standards.md`:
- Unit tests: ≥70%
- Integration tests for critical paths
- E2E test if user-visible

Use:
- BE: xUnit + Moq + Testcontainers
- Mobile: flutter_test + Mocktail
- Web: Vitest + React Testing Library

Cover:
- Happy path
- Validation failures
- Auth failures
- External API failures (mocked)
- Edge cases from module's "Edge Cases" section
```

---

## 15. Performance investigation

```
Performance issue in {area}.

Current metrics:
- {metric}
- {metric}

Target metrics from `02-tech-stack.md`:
- {target}

Please:
1. Suggest profiling approach
2. Hypothesize root causes
3. Suggest investigation steps
4. Propose optimizations (with tradeoffs)
```
