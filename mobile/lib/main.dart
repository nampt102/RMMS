import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:hive_flutter/hive_flutter.dart';

import 'app.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Portrait only for PG/Leader screens (camera + check-in flow is portrait).
  await SystemChrome.setPreferredOrientations([DeviceOrientation.portraitUp]);

  // Local storage (Hive) — for offline form drafts and small caches.
  await Hive.initFlutter();

  runApp(const ProviderScope(child: RmmsApp()));
}
