import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'app.dart';
import 'core/notifications/fcm_service.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Init Firebase before login/session restore so /auth/login can send fcmToken.
  await FcmService().initialize();

  // Use bundled fonts in google_fonts/ — avoids runtime HTTPS fetch (fails on
  // corp/VPN networks with SSL inspection when testing on physical devices).
  GoogleFonts.config.allowRuntimeFetching = false;

  // Portrait only for PG/Leader screens (camera + check-in flow is portrait).
  await SystemChrome.setPreferredOrientations([DeviceOrientation.portraitUp]);

  // Local storage (Hive) — for offline form drafts and small caches.
  await Hive.initFlutter();

  // Locale-aware date formatting (vi + en) for schedule/attendance screens.
  await initializeDateFormatting();

  // Register the FCM background handler before runApp. Guarded so a build
  // without Firebase config still launches (the handler self-initializes).
  try {
    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
  } catch (_) {
    // Firebase plugin not configured on this build — push is disabled.
  }

  runApp(const ProviderScope(child: RmmsApp()));
}
