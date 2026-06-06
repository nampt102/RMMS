import 'package:flutter/material.dart';

import '../theme/app_palette.dart';

/// Tone for [StatusPill] / [IconBadge] — maps to the [AppSemantics] families.
enum BrandTone { brand, success, warning, danger, info, neutral }

({Color fg, Color bg}) _toneColors(BuildContext context, BrandTone tone) {
  final s = context.semantics;
  final scheme = context.scheme;
  return switch (tone) {
    BrandTone.brand => (fg: scheme.primary, bg: scheme.primary.withValues(alpha: 0.12)),
    BrandTone.success => (fg: s.onSuccessContainer, bg: s.successContainer),
    BrandTone.warning => (fg: s.onWarningContainer, bg: s.warningContainer),
    BrandTone.danger => (fg: scheme.error, bg: scheme.error.withValues(alpha: 0.12)),
    BrandTone.info => (fg: s.info, bg: s.infoContainer),
    BrandTone.neutral => (
        fg: scheme.onSurfaceVariant,
        bg: scheme.surfaceContainerHighest,
      ),
  };
}

/// A rounded gradient header — the brand's signature surface. Used at the top of
/// the home dashboard and feature screens to anchor the visual identity.
class GradientHero extends StatelessWidget {
  const GradientHero({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.fromLTRB(20, 20, 20, 24),
    this.borderRadius = const BorderRadius.vertical(bottom: Radius.circular(28)),
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final BorderRadius borderRadius;

  @override
  Widget build(BuildContext context) {
    final s = context.semantics;
    return DecoratedBox(
      decoration: BoxDecoration(
        gradient: s.brandGradient,
        borderRadius: borderRadius,
      ),
      child: Padding(padding: padding, child: child),
    );
  }
}

/// Rounded icon chip with a tone-tinted background.
class IconBadge extends StatelessWidget {
  const IconBadge(
    this.icon, {
    super.key,
    this.tone = BrandTone.brand,
    this.size = 44,
  });

  final IconData icon;
  final BrandTone tone;
  final double size;

  @override
  Widget build(BuildContext context) {
    final c = _toneColors(context, tone);
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: c.bg,
        borderRadius: BorderRadius.circular(size * 0.32),
      ),
      child: Icon(icon, color: c.fg, size: size * 0.5),
    );
  }
}

/// Small status pill (icon + label) with semantic tone colours.
class StatusPill extends StatelessWidget {
  const StatusPill({
    super.key,
    required this.label,
    this.icon,
    this.tone = BrandTone.neutral,
  });

  final String label;
  final IconData? icon;
  final BrandTone tone;

  @override
  Widget build(BuildContext context) {
    final c = _toneColors(context, tone);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: c.bg,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 14, color: c.fg),
            const SizedBox(width: 5),
          ],
          Text(
            label,
            style: TextStyle(
              color: c.fg,
              fontSize: 12.5,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}

/// A floating surface with the brand's soft shadow (for emphasis cards).
class SoftCard extends StatelessWidget {
  const SoftCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(16),
    this.onTap,
    this.gradient,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final VoidCallback? onTap;
  final Gradient? gradient;

  @override
  Widget build(BuildContext context) {
    final scheme = context.scheme;
    final radius = BorderRadius.circular(20);
    return DecoratedBox(
      decoration: BoxDecoration(
        color: gradient == null ? scheme.surface : null,
        gradient: gradient,
        borderRadius: radius,
        boxShadow: context.semantics.cardShadow,
      ),
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: onTap,
          borderRadius: radius,
          child: Padding(padding: padding, child: child),
        ),
      ),
    );
  }
}

/// Tappable feature tile for the home grid: tinted icon badge + title + subtitle.
class FeatureTile extends StatelessWidget {
  const FeatureTile({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
    this.tone = BrandTone.brand,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;
  final BrandTone tone;

  @override
  Widget build(BuildContext context) {
    final scheme = context.scheme;
    final radius = BorderRadius.circular(20);
    return DecoratedBox(
      decoration: BoxDecoration(
        color: scheme.surface,
        borderRadius: radius,
        border: Border.all(color: scheme.outlineVariant),
      ),
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: onTap,
          borderRadius: radius,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                IconBadge(icon, tone: tone),
                const Spacer(),
                const SizedBox(height: 12),
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  subtitle,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontSize: 12.5,
                    height: 1.3,
                    color: scheme.onSurfaceVariant,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Small uppercase section label above a group of cards.
class SectionLabel extends StatelessWidget {
  const SectionLabel(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(
      text.toUpperCase(),
      style: TextStyle(
        fontSize: 12,
        fontWeight: FontWeight.w700,
        letterSpacing: 0.6,
        color: context.scheme.onSurfaceVariant,
      ),
    );
  }
}
