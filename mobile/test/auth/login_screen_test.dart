import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:rmms/features/auth/data/auth_repository.dart';
import 'package:rmms/features/auth/domain/auth_user.dart';
import 'package:rmms/features/auth/presentation/screens/login_screen.dart';
import 'package:rmms/l10n/generated/app_localizations.dart';

class _MockAuthRepository extends Mock implements AuthRepository {}

void main() {
  late _MockAuthRepository repo;

  const testUser = AuthUser(
    id: 'u1',
    email: 'pg@example.com',
    fullName: 'PG One',
    role: UserRole.pg,
  );

  setUp(() {
    repo = _MockAuthRepository();
    // Bootstrap path: no stored session.
    when(() => repo.hasSession()).thenAnswer((_) async => false);
    when(() => repo.login(
          email: any(named: 'email'),
          password: any(named: 'password'),
        )).thenAnswer((_) async => testUser);
  });

  Future<void> pump(WidgetTester tester) {
    return tester.pumpWidget(
      ProviderScope(
        overrides: [authRepositoryProvider.overrideWithValue(repo)],
        child: const MaterialApp(
          locale: Locale('vi'),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: LoginScreen(),
        ),
      ),
    );
  }

  testWidgets('renders email + password fields and the sign-in button',
      (tester) async {
    await pump(tester);
    expect(find.byType(TextFormField), findsNWidgets(2));
    expect(find.widgetWithText(FilledButton, 'Đăng nhập'), findsOneWidget);
  });

  testWidgets('blocks submit and shows a validation error on a bad email',
      (tester) async {
    await pump(tester);
    await tester.enterText(find.byType(TextFormField).at(0), 'not-an-email');
    await tester.enterText(find.byType(TextFormField).at(1), 'password1');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pump();

    expect(find.text('Email không hợp lệ.'), findsOneWidget);
    verifyNever(() => repo.login(
          email: any(named: 'email'),
          password: any(named: 'password'),
        ));
  });

  testWidgets('calls the repository once with valid credentials',
      (tester) async {
    await pump(tester);
    await tester.enterText(find.byType(TextFormField).at(0), 'pg@example.com');
    await tester.enterText(find.byType(TextFormField).at(1), 'password1');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();

    verify(() => repo.login(email: 'pg@example.com', password: 'password1'))
        .called(1);
  });
}
