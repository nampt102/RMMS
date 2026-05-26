# RMMS Mobile (PG & Leader)

Flutter 3.22+ · Riverpod 2 · Dio · Hive · Freezed · go_router · ARB-based l10n (vi/en).

Platform folders (`android/`, `ios/`, `linux/`, `macos/`, `windows/`) live **in this directory** — not at the monorepo root.

## Quickstart

```bash
cd mobile
flutter pub get
dart run build_runner build --delete-conflicting-outputs   # codegen: freezed + riverpod + json + retrofit
flutter gen-l10n                                            # ARB → AppLocalizations
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
```

For physical Android device, replace `10.0.2.2` with your machine's LAN IP.

**Application ID / bundle ID:** `com.rmms`

## Re-generate platform folders (rare)

Only if you intentionally need to refresh native scaffolding (will NOT overwrite `lib/`):

```bash
cd mobile
flutter create --org com.rmms --project-name rmms .
```

## Project Layout

```
mobile/
├── android/ ios/ linux/ macos/ windows/   # Flutter platform runners
├── pubspec.yaml
├── lib/
│   ├── main.dart
│   ├── app.dart
│   ├── core/ …
│   ├── features/ …
│   └── l10n/ …
├── assets/images/ assets/icons/
└── test/
```

## Conventions

See `../knowledge-base/08-coding-standards.md` (Mobile section).

- Riverpod 2.x for state; one provider per file
- Freezed for immutable models
- ARB-based l10n; never hardcode user-visible strings
- All paths/routes go through `AppRoutes` constants in `core/router/app_router.dart`
- Error codes (not raw messages) drive UX branching — map codes → ARB keys
