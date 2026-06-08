import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';

/// Reusable date / time field card used by Leave + OT forms.
/// Layout matches §6 of the redesign README: IconTile + label + big value
/// (Space Grotesk 17/800) + chevron, pressable.
class DateFieldCard extends StatelessWidget {
  const DateFieldCard({
    super.key,
    required this.label,
    required this.value,
    required this.onTap,
    this.tone = AppTone.indigo,
    this.icon = Icons.calendar_today_rounded,
  });

  final String label;
  final String value;
  final VoidCallback onTap;
  final AppTone tone;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      child: Row(
        children: [
          AppIconTile(icon: icon, tone: tone, size: 46, radius: 14),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  label,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.muted,
                    fontSize: 12.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.ink,
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.3,
                  ),
                ),
              ],
            ),
          ),
          const Icon(Icons.chevron_right_rounded,
              color: AppPalette.faint, size: 22),
        ],
      ),
    );
  }
}

/// White textarea card (min-height ~110) with hint + live char counter.
class ReasonField extends StatelessWidget {
  const ReasonField({
    super.key,
    required this.controller,
    required this.hint,
    this.maxLength = 1000,
  });

  final TextEditingController controller;
  final String hint;
  final int maxLength;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final count = controller.text.characters.length;
    return Container(
      decoration: BoxDecoration(
        color: AppPalette.surface,
        borderRadius: BorderRadius.circular(20),
        boxShadow: context.semantics.shadowSm,
      ),
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 84),
            child: TextField(
              controller: controller,
              maxLength: maxLength,
              maxLines: null,
              minLines: 4,
              style: GoogleFonts.plusJakartaSans(
                color: AppPalette.ink,
                fontSize: 14.5,
                fontWeight: FontWeight.w500,
                height: 1.45,
              ),
              decoration: InputDecoration(
                hintText: hint,
                hintStyle: GoogleFonts.plusJakartaSans(
                  color: AppPalette.faint,
                  fontSize: 14.5,
                  fontWeight: FontWeight.w500,
                ),
                isCollapsed: true,
                contentPadding: EdgeInsets.zero,
                border: InputBorder.none,
                enabledBorder: InputBorder.none,
                focusedBorder: InputBorder.none,
                counterText: '',
              ),
            ),
          ),
          const SizedBox(height: 6),
          Text(
            l.requestCounter(count, maxLength),
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.faint,
              fontSize: 11.5,
              fontWeight: FontWeight.w700,
              fontFeatures: const [FontFeature.tabularFigures()],
            ),
          ),
        ],
      ),
    );
  }
}
