import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/news_repository.dart';
import '../../domain/news_models.dart';

/// News detail (M14, AC-34): renders the article, marks it read on open, and — for important
/// news — shows a confirm (acknowledge) action that stays until confirmed.
class NewsDetailScreen extends ConsumerStatefulWidget {
  const NewsDetailScreen({super.key, required this.id});
  final String id;

  @override
  ConsumerState<NewsDetailScreen> createState() => _NewsDetailScreenState();
}

class _NewsDetailScreenState extends ConsumerState<NewsDetailScreen> {
  bool _markedRead = false;
  bool _confirming = false;

  void _markReadOnce() {
    if (_markedRead) return;
    _markedRead = true;
    Future(() async {
      try {
        await ref.read(newsRepositoryProvider).markRead(widget.id);
        ref.invalidate(myNewsProvider);
      } catch (_) {
        // best-effort; read state is non-critical
      }
    });
  }

  Future<void> _confirm() async {
    final l = AppLocalizations.of(context);
    setState(() => _confirming = true);
    try {
      await ref.read(newsRepositoryProvider).confirm(widget.id);
      ref.invalidate(myNewsProvider);
      if (mounted) showAppToast(context, message: l.newsConfirmed, kind: AppToastKind.success);
    } on ApiException catch (e) {
      if (mounted) showAppToast(context, message: e.message, kind: AppToastKind.error);
    } finally {
      if (mounted) setState(() => _confirming = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final async = ref.watch(myNewsProvider);

    NewsItem? item;
    final list = async.valueOrNull;
    if (list != null) {
      final matches = list.where((n) => n.id == widget.id);
      item = matches.isEmpty ? null : matches.first;
      if (item != null) _markReadOnce();
    }

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.newsDetailTitle),
            Expanded(
              child: async.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (_, __) => Center(child: Text(l.commonError, style: const TextStyle(color: AppPalette.muted))),
                data: (_) {
                  final n = item;
                  if (n == null) {
                    return Center(child: Text(l.newsEmpty, style: const TextStyle(color: AppPalette.muted)));
                  }
                  return ListView(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                    children: [
                      if (n.isImportant)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 10),
                          child: AppChip(label: l.newsImportant, tone: AppTone.rose, icon: Icons.priority_high_rounded),
                        ),
                      Text(
                        n.localizedTitle(lang),
                        style: GoogleFonts.spaceGrotesk(
                          color: AppPalette.ink,
                          fontSize: 22,
                          fontWeight: FontWeight.w800,
                          letterSpacing: -0.3,
                          height: 1.25,
                        ),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        n.localizedContent(lang),
                        style: const TextStyle(color: AppPalette.ink, fontSize: 15, height: 1.55),
                      ),
                    ],
                  );
                },
              ),
            ),
            if (item != null && item.needsAction)
              SafeArea(
                top: false,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                  child: AppButton.primary(
                    label: l.newsConfirm,
                    icon: Icons.check_circle_rounded,
                    loading: _confirming,
                    onPressed: _confirm,
                  ),
                ),
              )
            else if (item != null && item.isImportant && item.isConfirmed)
              SafeArea(
                top: false,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.check_circle_rounded, color: AppPalette.emerald, size: 18),
                      const SizedBox(width: 6),
                      Text(l.newsConfirmedLabel, style: const TextStyle(color: AppPalette.emerald, fontWeight: FontWeight.w600)),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
