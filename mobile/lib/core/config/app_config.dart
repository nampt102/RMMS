/// Build-time configuration. Override per environment via `--dart-define`:
///   flutter run --dart-define=API_BASE_URL=https://api-staging.rmms.example.com
class AppConfig {
  AppConfig._();

  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5080', // Android emulator → host machine
  );

  static const String sentryDsn = String.fromEnvironment(
    'SENTRY_DSN',
    defaultValue: '',
  );

  /// Geofence radius for check-in proximity check (BR-204).
  static const double checkInRadiusMeters = 150;

  /// Maximum allowed minutes before shift start (BR-209).
  static const int checkInEarlyToleranceMinutes = 60;
}
