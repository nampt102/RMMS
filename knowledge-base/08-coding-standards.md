# 08 — Coding Standards

Conventions for all RMMS code. Designed to be AI-friendly: clear, consistent, predictable.

## General Principles

1. **Explicit over implicit** — write code that's obvious to read
2. **Type-safe everywhere** — use TS/C# types, avoid `any`/`dynamic`
3. **No magic numbers** — extract constants
4. **Fail fast** — validate early, throw clear exceptions
5. **Idempotency by default** for write operations
6. **i18n-ready** — don't hardcode strings

## Backend (.NET / C#)

### Naming
- Classes / Records / Interfaces: `PascalCase`
- Interfaces: prefix `I` (`IUserRepository`)
- Async methods: suffix `Async`
- Private fields: `_camelCase`
- Constants: `PascalCase` (or `ALL_CAPS` for true constants)
- Method params, locals: `camelCase`
- Database entities: `PascalCase` class → `snake_case` table via EF Core convention

### Project Layout (already in `02-tech-stack.md`)

### Coding Style
- **Records** for DTOs and value objects (immutable)
- **Sealed classes** by default
- **`var`** OK when type obvious; explicit type when ambiguous
- **File-scoped namespaces**
- **Nullable reference types enabled** project-wide

```csharp
// Good
public sealed record CheckInRequest(
    Guid StoreId,
    Guid ShiftId,
    GpsCoordinate Gps,
    string? Note
);

// Bad — class with setters when immutable would do
public class CheckInRequest
{
    public Guid StoreId { get; set; }
    // ...
}
```

### Error handling
- Domain errors: throw custom exceptions (`StoreNotAssignedException`)
- Validation: FluentValidation, NOT manual `if` chains
- Don't catch `Exception` unless logging+rethrowing
- Use `Result<T,Error>` pattern only if team agrees consistently (default: exceptions)

### Async
- Always `async`/`await`, never `.Result` or `.Wait()`
- `ConfigureAwait(false)` in libraries (not in API/handlers)
- `CancellationToken` parameter on all async public methods

### Dependency Injection
- Constructor injection only
- Register interfaces, not concretes (except composition root)
- Scoped lifetime by default for handlers/services

### MediatR pattern
```csharp
public sealed record CheckInCommand(Guid UserId, CheckInRequest Request)
    : IRequest<CheckInResponse>;

public sealed class CheckInCommandHandler 
    : IRequestHandler<CheckInCommand, CheckInResponse>
{
    private readonly IAttendanceService _attendance;
    public CheckInCommandHandler(IAttendanceService attendance) 
        => _attendance = attendance;

    public async Task<CheckInResponse> Handle(
        CheckInCommand cmd, CancellationToken ct)
    {
        // ...
    }
}
```

### Validation
```csharp
public sealed class CheckInRequestValidator 
    : AbstractValidator<CheckInRequest>
{
    public CheckInRequestValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Gps).NotNull().SetValidator(new GpsValidator());
    }
}
```

### EF Core
- Use `DbSet<T>` properties
- Configure entities via `IEntityTypeConfiguration<T>` (not inline)
- Avoid lazy loading (disable globally)
- `AsNoTracking()` for read-only queries
- `Include()` explicit for joins

### Tests
- xUnit + FluentAssertions
- Arrange / Act / Assert sections separated by blank line
- One assertion concept per test (multiple `.Should()` chained OK)
- Integration tests via Testcontainers (real PostgreSQL)
- Mocks via Moq

```csharp
[Fact]
public async Task CheckIn_WithFakeGps_ShouldBlock()
{
    // Arrange
    var sut = CreateSut();
    var request = new CheckInRequest(StoreId, ShiftId, FakeGps, null);

    // Act
    var act = () => sut.Handle(new CheckInCommand(UserId, request), default);

    // Assert
    await act.Should().ThrowAsync<FakeGpsDetectedException>();
}
```

## Frontend (Next.js / TypeScript)

### Naming
- Components: `PascalCase` (`UserList`, `AttendanceCard`)
- Hooks: `camelCase` with `use` prefix (`useAttendance`)
- Files: kebab-case (`user-list.tsx`, `use-attendance.ts`)
- Types/interfaces: `PascalCase` (`User`, `AttendanceStatus`)
- Constants: `SCREAMING_SNAKE_CASE`
- Boolean: `is*`, `has*`, `should*`

### Style
- Function components only (no class components)
- Arrow function: `const Component = () => {...}`
- Props type explicitly defined, not inline
- No default exports for components (named exports help refactoring)

```tsx
// Good
type AttendanceCardProps = {
  record: AttendanceRecord;
  onReview?: () => void;
};

export const AttendanceCard = ({ record, onReview }: AttendanceCardProps) => {
  return <div>...</div>;
};

// Avoid
export default function AttendanceCard(props: any) {
  return <div>...</div>;
}
```

### State management
- Server state → TanStack Query
- Local UI state → `useState`
- Cross-component client state → Zustand store
- Forms → React Hook Form + Zod

### Folder structure per feature
```
features/attendance/
├── api/
│   ├── get-attendance.ts        // useQuery hook
│   └── check-in.ts              // useMutation hook
├── components/
│   ├── attendance-list.tsx
│   └── attendance-card.tsx
├── hooks/
│   └── use-attendance-status.ts
├── types/
│   └── attendance.ts
└── utils/
    └── status-color.ts
```

### Tailwind / Ant Design Pro
- Use Ant Design Pro components first (`ProTable`, `ProForm`, etc.)
- Custom Tailwind only when AntD doesn't fit
- No inline styles unless dynamic value

### i18n
- All user-visible strings via `useTranslations()` from next-intl
- Key naming: `featureName.subKey` (`attendance.checkIn.title`)
- Never concatenate translated strings

```tsx
// Good
const t = useTranslations("attendance");
return <h1>{t("checkIn.title")}</h1>;

// Bad
return <h1>{lang === "vi" ? "Chấm công" : "Check-in"}</h1>;
```

### Error handling
- API errors → caught in TanStack Query, display via toast or inline
- Use `error.code` (string) for branching logic, not `error.message`
- Don't display raw error messages to user — map to localized strings

### Testing
- Vitest + React Testing Library
- Test user behavior, not implementation
- Mock API calls via MSW

```tsx
test("user can submit check-in", async () => {
  render(<CheckInPage />);
  await userEvent.click(screen.getByRole("button", { name: /check in/i }));
  expect(await screen.findByText(/success/i)).toBeInTheDocument();
});
```

## Mobile (Flutter / Dart)

### Naming
- Classes: `PascalCase`
- Variables, methods, params: `camelCase`
- Constants: `lowerCamelCase` with `k` prefix (`kPrimaryColor`)
- Files: `snake_case.dart`
- Folders: `snake_case`

### Style
- Use `late` for late-init, prefer constructors
- Prefer `final` and `const` aggressively
- Null safety always
- `dartfmt` (built-in `dart format`)

### State (Riverpod 2.x)
- One provider per file in `lib/features/.../providers/`
- Use `@riverpod` code-gen annotation (cleaner than manual)
- Async providers via `FutureProvider` / `StreamProvider`

```dart
@riverpod
class AttendanceList extends _$AttendanceList {
  @override
  Future<List<Attendance>> build() async {
    final api = ref.read(apiClientProvider);
    return await api.getAttendanceList();
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final api = ref.read(apiClientProvider);
      return await api.getAttendanceList();
    });
  }
}
```

### Folder structure per feature
```
lib/features/attendance/
├── data/
│   ├── attendance_repository.dart
│   └── attendance_api.dart       // retrofit interface
├── domain/
│   ├── attendance.dart           // freezed model
│   └── attendance_status.dart    // enum
├── presentation/
│   ├── screens/
│   │   ├── check_in_screen.dart
│   │   └── attendance_list_screen.dart
│   ├── widgets/
│   │   └── attendance_card.dart
│   └── providers/
│       └── attendance_list_provider.dart
└── attendance.dart  // barrel file
```

### Models (Freezed)
```dart
@freezed
class Attendance with _$Attendance {
  const factory Attendance({
    required String id,
    required String userId,
    required DateTime checkInAt,
    DateTime? checkOutAt,
    required AttendanceStatus status,
  }) = _Attendance;

  factory Attendance.fromJson(Map<String, dynamic> json) =>
      _$AttendanceFromJson(json);
}
```

### Navigation (go_router)
- Centralize routes in `lib/core/router/app_router.dart`
- Use named routes via constants
- Deep links supported for notifications

### i18n (Flutter intl)
- Strings in `lib/l10n/app_en.arb` and `app_vi.arb`
- Access via `AppLocalizations.of(context)!.attendanceCheckInTitle`
- No hardcoded strings in widgets

### Error handling
- Use `AsyncValue` from Riverpod for loading/error/data states
- Show errors via `ScaffoldMessenger` or inline
- Map error codes (not raw messages)

### Tests
- Unit: `flutter_test` for logic
- Widget: `flutter_test` with `WidgetTester`
- Integration: `integration_test` package
- Mocking: `mocktail`

## Database

### Naming (PostgreSQL)
- Tables: `snake_case`, plural (`users`, `attendance_records`)
- Columns: `snake_case`
- PK: `id`
- FK: `<entity>_id` (`user_id`, `store_id`)
- Timestamps: `created_at`, `updated_at`, `deleted_at`
- Booleans: `is_*` or `has_*`
- Indexes: `ix_<table>_<columns>`
- Foreign keys: `fk_<table>_<column>`

### Migrations
- One migration per logical change
- Migration name: `YYYYMMDD_HHMM_short_description` via EF Core
- Always test rollback (`Down` method must work)
- Don't squash migrations during Phase 1

### Conventions
- All timestamps: `timestamptz` (not `timestamp`)
- All money: `numeric(15,2)` if added (Phase 2)
- Use `uuid` PK with `uuid_generate_v7()` (need extension or app-generated)
- JSONB columns: validate schema in application

## Git

### Branch strategy
- `main` — production
- `develop` — integration
- `feature/<sprint>-<short-desc>` — features
- `fix/<issue>-<short-desc>` — bug fixes
- `chore/<desc>` — non-functional changes

### Commit messages (Conventional Commits)
```
<type>(<scope>): <description>

<body>

<footer>
```

Types: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `perf`, `style`, `build`, `ci`

Examples:
- `feat(attendance): add fake GPS detection`
- `fix(approval): handle email token expiry`
- `chore(deps): bump .NET to 8.0.5`

### PR Rules
- Title follows commit convention
- Description: what, why, how to test, screenshots if UI
- At least 1 reviewer approval (the other dev)
- All CI checks pass
- No `console.log` / `Console.WriteLine` / `print` left in code
- No commented-out code

## Documentation

- README.md per repo with setup, run, test
- API endpoints documented in Swagger (auto)
- Domain decisions in `/docs/decisions/` as ADRs (Architecture Decision Records)
- Knowledge base updated when business rules change

## ADR (Architecture Decision Record) format
```
# ADR-<num>: <title>

## Status
Accepted / Proposed / Deprecated / Superseded by ADR-X

## Context
What forces are at play?

## Decision
What we decided.

## Consequences
Good and bad.
```

Store in `/docs/decisions/ADR-XXX-title.md`.
