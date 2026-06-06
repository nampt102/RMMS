import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Background isolate handler for FCM. Must be a top-level (or static) function
/// annotated `@pragma('vm:entry-point')` so it survives tree-shaking and can be
/// invoked when the app is terminated/background.
///
/// We only re-initialize Firebase here; the OS renders the notification tray
/// entry itself, and any data payload (e.g. device approval) is acted on when
/// the user taps the notification (`onMessageOpenedApp` / `getInitialMessage`).
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  try {
    await Firebase.initializeApp();
  } catch (_) {
    // Firebase not configured on this build — nothing to do in the background.
  }
}

/// Thin, fail-safe wrapper around `firebase_messaging`.
///
/// Firebase config files (`google-services.json` / `GoogleService-Info.plist`)
/// are provisioned per build via `flutterfire configure`. When they are absent
/// (e.g. a local dev build before Firebase is set up), [initialize] degrades
/// gracefully: [isAvailable] stays `false` and every call becomes a safe no-op,
/// so the app still runs and login simply omits the FCM token.
///
/// Server-side push *delivery* lands in M14 Notification; this client only
/// acquires the token (sent on login per BR-105 device flow) and reacts to
/// incoming messages.
class FcmService {
  bool _available = false;
  bool get isAvailable => _available;

  /// Guarded Firebase init. Safe to call once at startup.
  Future<void> initialize() async {
    try {
      await Firebase.initializeApp();
      _available = true;
    } catch (e) {
      _available = false;
      debugPrint('FcmService: Firebase unavailable, FCM disabled ($e)');
    }
  }

  /// Prompts the user for notification permission (iOS + Android 13+).
  /// No-op when Firebase is unavailable.
  Future<void> requestPermission() async {
    if (!_available) return;
    try {
      await FirebaseMessaging.instance.requestPermission();
    } catch (e) {
      debugPrint('FcmService: requestPermission failed ($e)');
    }
  }

  /// Current FCM registration token, or `null` if unavailable/denied.
  /// Sent in the `/auth/login` device payload so the server can later push
  /// device-change outcomes to this install.
  Future<String?> token() async {
    if (!_available) return null;
    try {
      return await FirebaseMessaging.instance.getToken();
    } catch (e) {
      debugPrint('FcmService: getToken failed ($e)');
      return null;
    }
  }

  /// Messages received while the app is in the foreground.
  Stream<RemoteMessage> get onForegroundMessage =>
      _available ? FirebaseMessaging.onMessage : const Stream<RemoteMessage>.empty();

  /// Fired when the user taps a notification that opened/resumed the app.
  Stream<RemoteMessage> get onMessageOpenedApp => _available
      ? FirebaseMessaging.onMessageOpenedApp
      : const Stream<RemoteMessage>.empty();

  /// Emits a new token whenever FCM rotates it. Re-registration with the server
  /// is wired in M14 (no update endpoint yet); for now callers may log it.
  Stream<String> get onTokenRefresh => _available
      ? FirebaseMessaging.instance.onTokenRefresh
      : const Stream<String>.empty();

  /// The message that cold-started the app via a notification tap, if any.
  Future<RemoteMessage?> initialMessage() async {
    if (!_available) return null;
    try {
      return await FirebaseMessaging.instance.getInitialMessage();
    } catch (e) {
      debugPrint('FcmService: getInitialMessage failed ($e)');
      return null;
    }
  }
}

final fcmServiceProvider = Provider<FcmService>((ref) => FcmService());
