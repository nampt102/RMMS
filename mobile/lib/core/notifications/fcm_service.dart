import 'dart:io';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config.dart';

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
  static Future<void>? _initFuture;
  static bool _firebaseReady = false;

  bool _available = false;
  bool _permissionRequested = false;
  bool get isAvailable => _available;

  /// Guarded Firebase init. Safe to call multiple times / from multiple instances.
  Future<void> initialize() async {
    if (_firebaseReady) {
      _available = true;
      return;
    }
    _initFuture ??= _doInitialize();
    await _initFuture;
    _available = _firebaseReady;
  }

  Future<void> _doInitialize() async {
    try {
      if (Firebase.apps.isEmpty) {
        await Firebase.initializeApp();
      }
      _firebaseReady = true;
      debugPrint('FcmService: Firebase initialized');
    } catch (e) {
      _firebaseReady = false;
      debugPrint('FcmService: Firebase unavailable, FCM disabled ($e)');
    }
  }

  /// iOS needs notification permission + APNs token before FCM returns a token.
  Future<void> ensureReadyForToken() async {
    await initialize();
    if (!_available) return;

    if (!_permissionRequested) {
      await requestPermission();
      _permissionRequested = true;
    }

    if (Platform.isIOS) {
      await _waitForApnsToken();
    }
  }

  Future<void> _waitForApnsToken() async {
    for (var i = 0; i < 10; i++) {
      try {
        final apns = await FirebaseMessaging.instance.getAPNSToken();
        if (apns != null) {
          debugPrint('FcmService: APNs token ready');
          return;
        }
      } catch (e) {
        debugPrint('FcmService: getAPNSToken attempt ${i + 1} ($e)');
      }
      await Future<void>.delayed(const Duration(milliseconds: 500));
    }
    debugPrint('FcmService: APNs token not available (check Push capability + entitlements)');
  }

  /// Prompts the user for notification permission (iOS + Android 13+).
  /// No-op when Firebase is unavailable.
  Future<void> requestPermission() async {
    if (!_available) return;
    try {
      final settings = await FirebaseMessaging.instance.requestPermission();
      debugPrint('FcmService: permission=${settings.authorizationStatus}');
    } catch (e) {
      debugPrint('FcmService: requestPermission failed ($e)');
    }
  }

  /// Current FCM registration token, or `null` if unavailable/denied.
  /// Sent in the `/auth/login` device payload so the server can later push
  /// device-change outcomes to this install.
  Future<String?> token() async {
    await ensureReadyForToken();
    if (!_available) return null;
    try {
      final t = await FirebaseMessaging.instance.getToken();
      if (t == null || t.isEmpty) {
        debugPrint('FcmService: getToken returned empty');
      } else {
        debugPrint('FcmService: getToken ok (${t.length} chars)');
      }
      return t;
    } catch (e) {
      final msg = e.toString();
      debugPrint('FcmService: getToken failed ($e)');
      if (msg.contains('TLS') ||
          msg.contains('SSL') ||
          msg.contains('certificate')) {
        debugPrint(
          'FcmService: iPhone không kết nối được Firebase (Google). '
          'Tắt VPN/proxy, thử 4G, hoặc mở firewall cho *.googleapis.com. '
          'API LAN (${AppConfig.apiBaseUrl}) vẫn chạy nhưng FCM token sẽ null.',
        );
      }
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
