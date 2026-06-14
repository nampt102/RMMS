import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/news_repository.dart';
import '../../domain/news_models.dart';

/// News list (M14, AC-33): published news assigned to the reader, newest first, with an
/// unread dot and an "important" tag.
class NewsListScreen extends ConsumerWidget {
  const NewsListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final news = ref.watch(myNewsProvider);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.newsTitle),
            Expanded(
              child: news.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (_, __) => Center(child: Text(l.commonError, style: const TextStyle(color: AppPalette.muted))),
                data: (items) {
                  if (items.isEmpty) {
                    return Center(
                      child: Padding(
                        padding: const EdgeInsets.all(32),
                        child: Text(l.newsEmpty, textAlign: TextAlign.center, style: const TextStyle(color: AppPalette.muted)),
                      ),
                    );
                  }
                  return RefreshIndicator(
                    onRefresh: () async => ref.invalidate(myNewsProvider),
                    child: ListView.separated(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                      itemCount: items.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 12),
                      itemBuilder: (context, i) => _NewsCard(news: items[i], lang: lang),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _NewsCard extends StatelessWidget {
  const _NewsCard({required this.news, required this.lang});
  final NewsItem news;
  final String lang;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final unread = !news.isRead || news.needsAction;
    return AppCard(
      onTap: () => context.push('${AppRoutes.news}/${news.id}'),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.only(top: 6),
            child: Container(
              width: 9,
              height: 9,
              decoration: BoxDecoration(
                color: unread ? AppPalette.indigo : Colors.transparent,
                shape: BoxShape.circle,
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  news.localizedTitle(lang),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.ink,
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.2,
                  ),
                ),
                const SizedBox(height: 6),
                Row(
                  children: [
                    if (news.isImportant) ...[
                      AppChip(label: l.newsImportant, tone: AppTone.rose, icon: Icons.priority_high_rounded),
                      const SizedBox(width: 8),
                    ],
                    if (news.category != null && news.category!.isNotEmpty)
                      AppChip(label: news.category!, tone: AppTone.neutral),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          const Icon(Icons.chevron_right_rounded, color: AppPalette.faint),
        ],
      ),
    );
  }
}
