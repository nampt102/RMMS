import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rmms/features/auth/data/auth_repository.dart';
import 'package:rmms/features/auth/presentation/screens/register_screen.dart';
import 'package:rmms/l10n/generated/app_localizations.dart';

class _MockAuthRepository extends Mock implements AuthRepository {}

void main() {
  late _MockAuthRepository repo;

  setUp(() {
    repo = _MockAuthRepository();
    when(() => repo.register(
          email: any(named: 'email'),
          password: any(named: 'password'),
          fullName: any(named: 'fullName'),
          phone: any(named: 'phone'),
          preferredLanguage: any(named: 'preferredLanguage'),
        )).thenAnswer((_) async {});
  });

  // The register screen navigates with `context.go` on success, so it needs a
  // real router. Destination screens are trivial placeholders.
  Future<void> pump(WidgetTester tester) {
    final router = GoRouter(
      initialLocation: '/register',
      routes: [
        GoRoute(path: '/register', builder: (_, __) => const RegisterScreen()),
        GoRoute(
          path: '/verify-email',
          builder: (_, __) => const Scaffold(body: Text('verify')),
        ),
        GoRoute(
          path: '/login',
          builder: (_, __) => const Scaffold(body: Text('login')),
        ),
      ],
    );

    return tester.pumpWidget(
      ProviderScope(
        overrides: [authRepositoryProvider.overrideWithValue(repo)],
        child: MaterialApp.router(
          locale: const Locale('vi'),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          routerConfig: router,
        ),
      ),
    );
  }

  Future<void> fillValid(WidgetTester tester, {required String confirm}) async {
    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'PG One'); // full name
    await tester.enterText(fields.at(1), 'pg@example.com'); // email
    await tester.enterText(fields.at(2), ''); // phone (optional)
    await tester.enterText(fields.at(3), 'password1'); // password
    await tester.enterText(fields.at(4), confirm); // confirm
  }

  testWidgets('blocks submit when the confirmation does not match',
      (tester) async {
    await pump(tester);
    await fillValid(tester, confirm: 'different1');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng ký'));
    await tester.pump();

    expect(find.text('Mật khẩu xác nhận không khớp.'), findsOneWidget);
    verifyNever(() => repo.register(
          email: any(named: 'email'),
          password: any(named: 'password'),
          fullName: any(named: 'fullName'),
          phone: any(named: 'phone'),
          preferredLanguage: any(named: 'preferredLanguage'),
        ));
  });

  testWidgets('registers and navigates on a valid submission', (tester) async {
    await pump(tester);
    await fillValid(tester, confirm: 'password1');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng ký'));
    await tester.pumpAndSettle();

    verify(() => repo.register(
          email: 'pg@example.com',
          password: 'password1',
          fullName: 'PG One',
          phone: '',
          preferredLanguage: 'vi',
        )).called(1);
    expect(find.text('verify'), findsOneWidget);
  });
}
