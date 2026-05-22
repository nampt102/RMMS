# Test Generation Prompts

Prompts to generate quality test cases for RMMS code.

---

## 1. Generate test plan for a module

```
Generate a comprehensive test plan for module {M5 Attendance & Anti-Fraud}.

Reference: `modules/M05-attendance-antifraud.md`

Organize as:

## Test Plan

### Unit Tests (BE, xUnit)
For each method/handler, list:
- Happy path
- Each validation failure
- Each business rule branch
- Each error condition

### Integration Tests (BE, Testcontainers)
- Real DB + Redis interactions
- Critical end-to-end flows
- Concurrency scenarios

### Mobile Widget Tests
- Each screen renders without crash
- User interactions trigger correct state changes
- Error/loading/empty states

### Mobile Integration Tests
- Full check-in flow with mocked API
- Offline draft → online sync

### Web Tests (Vitest + RTL)
- Each interactive component
- Form validation
- API error handling

### E2E Tests (Playwright)
- One per AC referenced in module

Output as a checklist with brief test descriptions.
```

---

## 2. Generate unit tests for a handler

```
Generate xUnit tests for this MediatR handler:

{paste handler code}

Conventions from `08-coding-standards.md`:
- One concept per test
- Arrange / Act / Assert separated
- FluentAssertions
- Moq for dependencies

Cover:
1. Happy path (all green)
2. Each `throw` in code
3. Each validation rule
4. Each business rule branch
5. Cancellation token honored
6. Repository called with expected args
7. Audit log written if applicable

For each test:
- Descriptive name: `Method_Condition_ExpectedResult`
- Clear setup
- Single assertion concept
- No magic literals (use named constants/variables)
```

---

## 3. Generate edge case tests

```
Given this code:

{paste}

What edge cases are NOT tested? Generate tests for each:

Consider:
- Boundary values (0, 1, max, max+1)
- Empty / null inputs
- Strings: empty, very long, special chars, Unicode, emojis
- Numbers: negative, zero, overflow
- Dates: leap year, DST transition, timezone edge
- Concurrency: simultaneous requests
- External failures: timeouts, 5xx responses, malformed responses
- Resource exhaustion: large payloads, many connections
- Authorization: wrong role, deleted user, expired token
- Data corruption: invalid foreign keys, broken constraints

Generate at least 8 distinct edge case tests.
```

---

## 4. Generate Flutter widget tests

```
Generate widget tests for this screen:

{paste widget code}

Using:
- flutter_test
- Mocktail for providers/repos
- ProviderScope.overrideWith for Riverpod overrides

Cover:
1. Renders without exception
2. Shows loading state initially
3. Shows data when loaded
4. Shows error state on failure
5. Shows empty state when no data
6. User taps button → expected action triggered
7. User submits form → API called with right args
8. Pull-to-refresh works
9. Localization: VI and EN render correctly
10. Navigation triggers happen as expected

For each:
- Use `testWidgets` with descriptive name
- Pump the widget with proper provider overrides
- Use `find.byType`, `find.text`, `find.byKey` appropriately
- `expect()` final state
```

---

## 5. Generate API integration tests

```
Generate integration tests for endpoint `{POST /api/v1/attendance/check-in}`.

Stack: xUnit + Testcontainers + WebApplicationFactory<Program>

Use a fresh PostgreSQL container per test class.

Cover:
1. Authenticated user with valid request → 201 + body
2. Anonymous → 401
3. Wrong role → 403
4. Missing required fields → 422 with error details
5. Store not assigned → 409 STORE_NOT_ASSIGNED
6. Fake GPS detected → 409 FAKE_GPS_DETECTED
7. Face API failure → 502 (or pending review status)
8. Idempotency key reused → returns cached response
9. Already checked in → 409 ALREADY_CHECKED_IN
10. Audit log entry created
11. Files uploaded to MinIO with expected key pattern

Setup:
- Seed user, store, schedule via test fixtures
- Mock external services (Face API, MinIO) at HTTP boundary

Assert:
- HTTP status code
- Response body shape
- DB state changes
- External service calls made
```

---

## 6. Generate Vitest tests for React component

```
Generate Vitest + React Testing Library tests for:

{paste component}

Cover:
1. Renders without crash
2. Renders with required props
3. Conditional rendering based on props/state
4. User events: click, type, select
5. Form submission with valid input
6. Form validation errors shown
7. Loading state while API call in flight (use MSW)
8. Success state after API success
9. Error toast after API error
10. Accessibility: roles, labels, aria attributes

Setup:
- Wrap with QueryClientProvider for TanStack Query
- Wrap with NextIntlProvider for i18n
- Use `userEvent` for interactions (not `fireEvent` for typing)
- Use MSW handlers in test setup

No tests for implementation details (CSS classes, internal state names).
Test what user experiences.
```

---

## 7. Generate Playwright E2E tests

```
Generate Playwright E2E tests for AC-{18 BUH email-link approval}.

Scenario (from acceptance criterion):
1. Leader creates a Visit Plan request
2. BUH receives email with approval link
3. BUH clicks link in fresh browser (not logged in)
4. BUH sees approval page
5. BUH clicks "Approve"
6. Confirmation page shown
7. Leader gets notification

Implementation:
- Use Playwright with TypeScript
- Use page object pattern
- Seed data via API before test (no UI for setup)
- Use a real email inbox catcher (Mailosaur/Inbucket) OR check DB for email_log entries with link
- Test parallel-safe (unique user IDs)

Provide:
1. Test file
2. Page object files
3. API helper for seeding
4. Cleanup logic
```

---

## 8. Load test scenario

```
Generate a k6 load test for {check-in endpoint}.

Target: simulate 500 concurrent users checking in at 9:00 AM.

Pattern:
- Ramp up from 0 to 500 over 1 minute
- Hold at 500 for 5 minutes
- Ramp down

Each VU:
1. Login (cache token across iterations)
2. Wait random 0-30s (simulate user prep)
3. Submit check-in with multipart data (selfie + store photo as base64 fixtures)
4. Check 2xx response

Thresholds:
- 95th percentile response time < 2000ms
- Error rate < 1%

Provide k6 script with proper:
- Helpers for login + token caching
- Fixture handling
- Custom metrics for face API latency
- Output suitable for CI (--out json=results.json)
```

---

## 9. Test data generation

```
Generate test data fixtures for module {M5 Attendance}.

Need:
- 50 PG users (varied: enrolled face, not enrolled, with leader, without)
- 5 Leaders (varied PG counts: 1-15 PGs)
- 2 BUH
- 1 Admin
- 30 Stores across 3 areas
- Schedules for 7 days
- Sample attendance records (mixed statuses)

Output:
1. SQL seed script for PostgreSQL
2. OR EF Core seeder class
3. JSON fixtures for mock APIs (Face, FCM, SendGrid)

Use deterministic UUIDs (e.g., from a name-based hash) so tests are reproducible.
```

---

## 10. Test plan for an acceptance criterion

```
Detailed test plan for AC-{N}: "{paste criterion}"

For each layer, design tests:

### Backend
1. Unit: {what isolated unit, with what inputs}
2. Integration: {what flow, with what state}
3. Contract: {what API contract, with what schemas}

### Mobile
1. Widget: {what screen, what interactions}
2. Integration: {what full flow}

### Web
1. Unit: {what component}
2. Integration: {what feature flow}

### E2E
1. {Full user journey across systems}

### Manual UAT
1. {Stakeholder-facing demo script with steps + expected results}

Each test description ≤ 1 line. Total count for AC-{N}.
```

---

## 11. Mutation testing prompt

```
For this code:

{paste}

What if we mutated it as follows? Generate tests that would catch each mutation:

Mutations to consider:
- Change `>` to `>=` or `<`
- Change `&&` to `||`
- Replace `true` with `false`
- Remove `return` statement
- Change loop bound (off-by-one)
- Remove `await`
- Catch and swallow exception
- Skip validation
- Skip authorization check

For each mutation, name a test that should fail if the mutation were applied.
```
