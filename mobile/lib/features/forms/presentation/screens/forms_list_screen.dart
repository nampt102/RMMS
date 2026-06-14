import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/forms_repository.dart';

/// Forms assigned to the current user (M10, AC-22). Tap → fill screen.
class FormsListScreen extends ConsumerWidget {
  const FormsListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final forms = ref.watch(myFormsProvider);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.formsTitle),
            Expanded(
              child: forms.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (_, __) => _Message(text: l.commonError),
                data: (items) {
                  if (items.isEmpty) return _Message(text: l.formsEmpty);
                  return RefreshIndicator(
                    onRefresh: () async => ref.invalidate(myFormsProvider),
                    child: ListView.separated(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                      itemCount: items.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 12),
                      itemBuilder: (context, i) {
                        final f = items[i];
                        return AppCard(
                          onTap: () => context.push('${AppRoutes.forms}/${f.formId}'),
                          child: Row(
                            children: [
                              const AppIconTile(icon: Icons.description_rounded, tone: AppTone.violet),
                              const SizedBox(width: 14),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      f.localizedName(lang),
                                      maxLines: 2,
                                      overflow: TextOverflow.ellipsis,
                                      style: GoogleFonts.spaceGrotesk(
                                        color: AppPalette.ink,
                                        fontSize: 16,
                                        fontWeight: FontWeight.w800,
                                        letterSpacing: -0.2,
                                      ),
                                    ),
                                    const SizedBox(height: 2),
                                    Text(
                                      '${f.code} · v${f.version}',
                                      style: const TextStyle(color: AppPalette.muted, fontSize: 13),
                                    ),
                                  ],
                                ),
                              ),
                              const Icon(Icons.chevron_right_rounded, color: AppPalette.faint),
                            ],
                          ),
                        );
                      },
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

class _Message extends StatelessWidget {
  const _Message({required this.text});
  final String text;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Text(text, textAlign: TextAlign.center, style: const TextStyle(color: AppPalette.muted)),
        ),
      );
}
