import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/documents_repository.dart';
import '../../domain/document_models.dart';

/// Document Center (M13, AC-31/32): list accessible documents (public + private), search by
/// name, and open via the system viewer (signed URL, ADR-017).
class DocumentsListScreen extends ConsumerStatefulWidget {
  const DocumentsListScreen({super.key});

  @override
  ConsumerState<DocumentsListScreen> createState() => _DocumentsListScreenState();
}

class _DocumentsListScreenState extends ConsumerState<DocumentsListScreen> {
  String _search = '';
  String? _opening;

  Future<void> _open(DocumentItem doc) async {
    final l = AppLocalizations.of(context);
    setState(() => _opening = doc.id);
    try {
      final url = await ref.read(documentsRepositoryProvider).downloadUrl(doc.id);
      final ok = await launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication);
      if (!ok && mounted) {
        showAppToast(context, message: l.docOpenFailed, kind: AppToastKind.error);
      }
    } on ApiException catch (e) {
      if (mounted) showAppToast(context, message: e.message, kind: AppToastKind.error);
    } catch (_) {
      if (mounted) showAppToast(context, message: l.docOpenFailed, kind: AppToastKind.error);
    } finally {
      if (mounted) setState(() => _opening = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final search = _search.trim().isEmpty ? null : _search.trim();
    final docs = ref.watch(myDocumentsProvider(search));

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.documentsTitle),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
              child: TextField(
                onChanged: (v) => setState(() => _search = v),
                decoration: InputDecoration(
                  prefixIcon: const Icon(Icons.search_rounded),
                  hintText: l.documentsSearchHint,
                  isDense: true,
                  border: const OutlineInputBorder(),
                ),
              ),
            ),
            Expanded(
              child: docs.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (_, __) => Center(child: Text(l.commonError, style: const TextStyle(color: AppPalette.muted))),
                data: (items) {
                  if (items.isEmpty) {
                    return Center(
                      child: Padding(
                        padding: const EdgeInsets.all(32),
                        child: Text(l.documentsEmpty, textAlign: TextAlign.center, style: const TextStyle(color: AppPalette.muted)),
                      ),
                    );
                  }
                  return RefreshIndicator(
                    onRefresh: () async => ref.invalidate(myDocumentsProvider(search)),
                    child: ListView.separated(
                      padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
                      itemCount: items.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 12),
                      itemBuilder: (context, i) {
                        final d = items[i];
                        return AppCard(
                          onTap: _opening == null ? () => _open(d) : null,
                          child: Row(
                            children: [
                              AppIconTile(
                                icon: d.isPrivate ? Icons.lock_rounded : Icons.insert_drive_file_rounded,
                                tone: d.isPrivate ? AppTone.amber : AppTone.indigo,
                              ),
                              const SizedBox(width: 14),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      d.name,
                                      maxLines: 2,
                                      overflow: TextOverflow.ellipsis,
                                      style: GoogleFonts.spaceGrotesk(
                                        color: AppPalette.ink,
                                        fontSize: 15,
                                        fontWeight: FontWeight.w800,
                                        letterSpacing: -0.2,
                                      ),
                                    ),
                                    const SizedBox(height: 2),
                                    Text('${d.sizeLabel} · ${d.mimeType}',
                                        style: const TextStyle(color: AppPalette.muted, fontSize: 12)),
                                  ],
                                ),
                              ),
                              const SizedBox(width: 8),
                              if (_opening == d.id)
                                const SizedBox(
                                    width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
                              else
                                const Icon(Icons.open_in_new_rounded, color: AppPalette.faint, size: 20),
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
