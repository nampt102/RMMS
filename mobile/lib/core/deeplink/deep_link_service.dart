import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../router/app_router.dart';

/// Listens for `rmms://` custom-scheme deep links and routes them to the
/// matching screen (M01, sprint-01 Day 4/6):
///   - `rmms://verify-email?token=...`   → verify-email screen
///   - `rmms://reset-password?token=...` → reset-password screen
///
/// https Universal Links / App Links are deferred to Sprint 02 (R-4); the
/// manual-code fallback on each screen covers the gap until then.
class DeepLinkService {
  DeepLinkService(this._router) : _appLinks = AppLinks();

  final GoRouter _router;
  final AppLinks _appLinks;
  StreamSubscription<Uri>? _sub;

  Future<void> init() async {
    // Cold start: the link that launched the app (if any).
    final initial = await _appLinks.getInitialLink();
    if (initial != null) _handle(initial);

    // Warm links while the app is already running.
    _sub = _appLinks.uriLinkStream.listen(_handle);
  }

  void _handle(Uri uri) {
    final token = Uri.encodeQueryComponent(uri.queryParameters['token'] ?? '');
    switch (uri.host) {
      case 'verify-email':
        _router.go('${AppRoutes.verifyEmail}?token=$token');
      case 'reset-password':
        _router.go('${AppRoutes.resetPassword}?token=$token');
      default:
        // Unknown link target — ignore rather than navigate somewhere wrong.
        break;
    }
  }

  Future<void> dispose() async {
    await _sub?.cancel();
    _sub = null;
  }
}

final deepLinkServiceProvider = Provider<DeepLinkService>((ref) {
  final service = DeepLinkService(ref.watch(appRouterProvider));
  ref.onDispose(service.dispose);
  return service;
});
