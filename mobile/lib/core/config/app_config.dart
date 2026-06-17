/// Build-time configuration. Override per environment via `--dart-define`:
///   flutter run --dart-define=API_BASE_URL=http://127.0.0.1:5080
class AppConfig {
  AppConfig._();

  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://103.216.116.206:5010',
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
