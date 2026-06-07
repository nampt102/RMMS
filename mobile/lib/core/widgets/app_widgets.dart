// RMMS Redesign 2026 — Design-System primitives.
//
// Mirrors the prototype's `ds.jsx`: AppChip, AppIconTile, AppCard, AppButton
// (primary / emerald / soft / destructive), AppTopBar, AppBottomSheetScaffold +
// `showAppSheet`, AppToast + `showAppToast`, and the glass AppBottomNav with a
// raised centre "Chấm công" FAB.
//
// All press feedback uses scale 0.965 over 140ms `cubic-bezier(.2,.8,.2,1)` and
// respects `MediaQuery.disableAnimations`. Token sources: `AppPalette` +
// `AppSemantics` ThemeExtension (see `lib/core/theme/`).

import 'dart:ui';

import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../theme/app_palette.dart';
import '../theme/app_theme.dart';

// ─────────────────────────────────────────────────────────────────────────────
// Tone — shared semantic palette for chips, tiles, icon backgrounds.
// ─────────────────────────────────────────────────────────────────────────────

enum AppTone { neutral, indigo, violet, emerald, amber, rose, sky }

({Color bg, Color fg}) chipColors(AppTone tone) => switch (tone) {
      AppTone.neutral => (bg: AppPalette.chipNeutralBg, fg: AppPalette.chipNeutralFg),
      AppTone.indigo => (bg: AppPalette.chipIndigoBg, fg: AppPalette.chipIndigoFg),
      AppTone.violet => (bg: AppPalette.tileVioletBg, fg: AppPalette.tileVioletFg),
      AppTone.emerald => (bg: AppPalette.chipEmeraldBg, fg: AppPalette.chipEmeraldFg),
      AppTone.amber => (bg: AppPalette.chipAmberBg, fg: AppPalette.chipAmberFg),
      AppTone.rose => (bg: AppPalette.chipRoseBg, fg: AppPalette.chipRoseFg),
      AppTone.sky => (bg: AppPalette.chipSkyBg, fg: AppPalette.chipSkyFg),
    };

({Color bg, Color fg}) tileColors(AppTone tone) => switch (tone) {
      AppTone.neutral => (bg: AppPalette.chipNeutralBg, fg: AppPalette.chipNeutralFg),
      AppTone.indigo => (bg: AppPalette.tileIndigoBg, fg: AppPalette.tileIndigoFg),
      AppTone.violet => (bg: AppPalette.tileVioletBg, fg: AppPalette.tileVioletFg),
      AppTone.emerald => (bg: AppPalette.tileEmeraldBg, fg: AppPalette.tileEmeraldFg),
      AppTone.amber => (bg: AppPalette.tileAmberBg, fg: AppPalette.tileAmberFg),
      AppTone.rose => (bg: AppPalette.tileRoseBg, fg: AppPalette.tileRoseFg),
      AppTone.sky => (bg: AppPalette.tileSkyBg, fg: AppPalette.tileSkyFg),
    };

// ─────────────────────────────────────────────────────────────────────────────
// Press-scale helper (0.965, 140ms, easeOutCubic).
// Honors MediaQuery.disableAnimations: collapses to a no-op when on.
// ─────────────────────────────────────────────────────────────────────────────

class PressScale extends StatefulWidget {
  const PressScale({
    super.key,
    required this.child,
    this.onTap,
    this.enabled = true,
    this.scale = 0.965,
  });

  final Widget child;
  final VoidCallback? onTap;
  final bool enabled;
  final double scale;

  @override
  State<PressScale> createState() => _PressScaleState();
}

class _PressScaleState extends State<PressScale> {
  bool _down = false;

  @override
  Widget build(BuildContext context) {
    final reduce = MediaQuery.disableAnimationsOf(context);
    final pressed = _down && widget.enabled && !reduce;
    final target = pressed ? widget.scale : 1.0;

    final child = AnimatedScale(
      scale: target,
      duration: const Duration(milliseconds: 140),
      curve: Curves.easeOutCubic,
      child: widget.child,
    );

    if (widget.onTap == null && widget.enabled == false) return child;

    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTapDown: (_) => setState(() => _down = true),
      onTapCancel: () => setState(() => _down = false),
      onTapUp: (_) => setState(() => _down = false),
      onTap: widget.enabled ? widget.onTap : null,
      child: child,
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// AppChip — pill, height 28, padding 0/11, 12.5/700.
// Variants: tonal (default) and `solid` (fg-colored fill + white text).
// ─────────────────────────────────────────────────────────────────────────────

class AppChip extends StatelessWidget {
  const AppChip({
    super.key,
    required this.label,
    this.icon,
    this.tone = AppTone.neutral,
    this.solid = false,
  });

  final String label;
  final IconData? icon;
  final AppTone tone;
  final bool solid;

  @override
  Widget build(BuildContext context) {
    final c = chipColors(tone);
    final bg = solid ? c.fg : c.bg;
    final fg = solid ? Colors.white : c.fg;
    return Container(
      height: 28,
      padding: const EdgeInsets.symmetric(horizontal: 11),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(999)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 14, color: fg),
            const SizedBox(width: 5),
          ],
          Text(
            label,
            style: GoogleFonts.plusJakartaSans(
              color: fg,
              fontSize: 12.5,
              fontWeight: FontWeight.w700,
              height: 1.0,
            ),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// AppIconTile — rounded square (default 48, radius 16) with tone-tinted bg
// and a centred stroke-style icon at ~50% of the tile size.
// ─────────────────────────────────────────────────────────────────────────────

class AppIconTile extends StatelessWidget {
  const AppIconTile({
    super.key,
    required this.icon,
    this.tone = AppTone.indigo,
    this.size = 48,
    this.radius = 16,
  });

  final IconData icon;
  final AppTone tone;
  final double size;
  final double radius;

  @override
  Widget build(BuildContext context) {
    final c = tileColors(tone);
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: c.bg,
        borderRadius: BorderRadius.circular(radius),
      ),
      child: Icon(icon, color: c.fg, size: size * 0.5),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// AppCard — surface, radius 28, shadow-sm, padding 16-18.
// `onTap` makes it pressable (scale 0.965).
// ─────────────────────────────────────────────────────────────────────────────

class AppCard extends StatelessWidget {
  const AppCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(18),
    this.borderRadius,
    this.onTap,
    this.color,
    this.gradient,
    this.shadow,
    this.border,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final BorderRadius? borderRadius;
  final VoidCallback? onTap;
  final Color? color;
  final Gradient? gradient;
  final List<BoxShadow>? shadow;
  final BoxBorder? border;

  @override
  Widget build(BuildContext context) {
    final scheme = context.scheme;
    final r = borderRadius ?? BorderRadius.circular(AppTheme.radiusXl);
    final decorated = DecoratedBox(
      decoration: BoxDecoration(
        color: gradient == null ? (color ?? scheme.surface) : null,
        gradient: gradient,
        borderRadius: r,
        boxShadow: shadow ?? context.semantics.shadowSm,
        border: border,
      ),
      child: Padding(padding: padding, child: child),
    );
    if (onTap == null) return decorated;
    return PressScale(onTap: onTap, child: decorated);
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// AppButton — height 54, radius 17, 16/700.
//
// Variants:
//   - AppButton.primary           grad-brand + shadow-brand, white text
//   - AppButton.emerald           grad-emerald, white text
//   - AppButton.soft              surface-2 fill, indigoDeep text
//   - AppButton.destructiveSoft   chipRoseBg fill, rose text
//
// Optional leading icon (20px). Loading shows a spinner instead of the label.
// ─────────────────────────────────────────────────────────────────────────────

enum _AppBtnVariant { primary, emerald, soft, destructive }

class AppButton extends StatelessWidget {
  const AppButton._({
    super.key,
    required this.label,
    required this.variant,
    this.icon,
    this.onPressed,
    this.loading = false,
    this.expand = true,
  });

  factory AppButton.primary({
    Key? key,
    required String label,
    IconData? icon,
    VoidCallback? onPressed,
    bool loading = false,
    bool expand = true,
  }) =>
      AppButton._(
        key: key,
        label: label,
        variant: _AppBtnVariant.primary,
        icon: icon,
        onPressed: onPressed,
        loading: loading,
        expand: expand,
      );

  factory AppButton.emerald({
    Key? key,
    required String label,
    IconData? icon,
    VoidCallback? onPressed,
    bool loading = false,
    bool expand = true,
  }) =>
      AppButton._(
        key: key,
        label: label,
        variant: _AppBtnVariant.emerald,
        icon: icon,
        onPressed: onPressed,
        loading: loading,
        expand: expand,
      );

  factory AppButton.soft({
    Key? key,
    required String label,
    IconData? icon,
    VoidCallback? onPressed,
    bool loading = false,
    bool expand = true,
  }) =>
      AppButton._(
        key: key,
        label: label,
        variant: _AppBtnVariant.soft,
        icon: icon,
        onPressed: onPressed,
        loading: loading,
        expand: expand,
      );

  factory AppButton.destructiveSoft({
    Key? key,
    required String label,
    IconData? icon,
    VoidCallback? onPressed,
    bool loading = false,
    bool expand = true,
  }) =>
      AppButton._(
        key: key,
        label: label,
        variant: _AppBtnVariant.destructive,
        icon: icon,
        onPressed: onPressed,
        loading: loading,
        expand: expand,
      );

  final String label;
  final _AppBtnVariant variant;
  final IconData? icon;
  final VoidCallback? onPressed;
  final bool loading;
  final bool expand;

  @override
  Widget build(BuildContext context) {
    final s = context.semantics;
    final enabled = onPressed != null && !loading;
    final radius = BorderRadius.circular(AppTheme.radiusMd);

    Gradient? gradient;
    Color? bg;
    Color fg;
    List<BoxShadow>? shadow;

    switch (variant) {
      case _AppBtnVariant.primary:
        gradient = s.brandGradient;
        fg = Colors.white;
        shadow = s.shadowBrand;
      case _AppBtnVariant.emerald:
        gradient = s.emeraldGradient;
        fg = Colors.white;
        shadow = const [
          BoxShadow(
            color: Color(0x6110B981), // emerald @ ~38%
            blurRadius: 30,
            offset: Offset(0, 10),
          ),
        ];
      case _AppBtnVariant.soft:
        bg = AppPalette.surface2;
        fg = AppPalette.indigoDeep;
      case _AppBtnVariant.destructive:
        bg = AppPalette.chipRoseBg;
        fg = AppPalette.chipRoseFg;
    }

    final content = loading
        ? SizedBox(
            width: 22,
            height: 22,
            child: CircularProgressIndicator(strokeWidth: 2.4, color: fg),
          )
        : Row(
            mainAxisSize: expand ? MainAxisSize.max : MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              if (icon != null) ...[
                Icon(icon, size: 20, color: fg),
                const SizedBox(width: 10),
              ],
              Flexible(
                child: Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  softWrap: false,
                  style: GoogleFonts.plusJakartaSans(
                    color: fg,
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    letterSpacing: -0.1,
                  ),
                ),
              ),
            ],
          );

    final box = Opacity(
      opacity: enabled ? 1 : 0.55,
      child: Container(
        height: AppTheme.controlHeight,
        width: expand ? double.infinity : null,
        padding: EdgeInsets.symmetric(horizontal: expand ? 16 : 22),
        decoration: BoxDecoration(
          gradient: gradient,
          color: bg,
          borderRadius: radius,
          boxShadow: enabled ? shadow : null,
        ),
        alignment: Alignment.center,
        child: content,
      ),
    );

    return PressScale(
      enabled: enabled,
      onTap: enabled ? onPressed : null,
      child: box,
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// AppTopBar — 40×40 back button (rounded 13, white surface, shadow-sm) +
// title 19/700 + optional trailing actions.
// Returns a Padding row; place it at the top of a Scaffold body.
// ─────────────────────────────────────────────────────────────────────────────

class AppTopBar extends StatelessWidget {
  const AppTopBar({
    super.key,
    required this.title,
    this.onBack,
    this.trailing = const [],
    this.padding = const EdgeInsets.fromLTRB(16, 8, 16, 8),
  });

  final String title;
  final VoidCallback? onBack;
  final List<Widget> trailing;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    final canPop = onBack != null || Navigator.canPop(context);
    return Padding(
      padding: padding,
      child: Row(
        children: [
          if (canPop)
            PressScale(
              onTap: onBack ?? () => Navigator.maybePop(context),
              child: Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: AppPalette.surface,
                  borderRadius: BorderRadius.circular(13),
                  boxShadow: context.semantics.shadowSm,
                ),
                alignment: Alignment.center,
                child: const Icon(Icons.chevron_left_rounded,
                    color: AppPalette.ink, size: 24),
              ),
            ),
          if (canPop) const SizedBox(width: 12),
          Expanded(
            child: Text(
              title,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: GoogleFonts.plusJakartaSans(
                color: AppPalette.ink,
                fontSize: 19,
                fontWeight: FontWeight.w700,
                letterSpacing: -0.3,
              ),
            ),
          ),
          ...trailing,
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Bottom Sheet — top radius 30, grab handle, slide-up, scrim rgba(20,19,40,.4).
// `showAppSheet` is the call-site helper.
// ─────────────────────────────────────────────────────────────────────────────

Future<T?> showAppSheet<T>({
  required BuildContext context,
  required WidgetBuilder builder,
  bool isScrollControlled = true,
}) {
  return showModalBottomSheet<T>(
    context: context,
    isScrollControlled: isScrollControlled,
    backgroundColor: Colors.transparent,
    barrierColor: const Color(0x66141428), // rgba(20,19,40,.4)
    builder: (ctx) => AppSheet(child: builder(ctx)),
  );
}

class AppSheet extends StatelessWidget {
  const AppSheet({super.key, required this.child, this.title});

  final Widget child;
  final String? title;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      top: false,
      child: Container(
        decoration: const BoxDecoration(
          color: AppPalette.surface,
          borderRadius: BorderRadius.vertical(top: Radius.circular(30)),
        ),
        padding: const EdgeInsets.fromLTRB(20, 10, 20, 22),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Grab handle.
            Container(
              width: 44,
              height: 5,
              margin: const EdgeInsets.only(bottom: 14),
              decoration: BoxDecoration(
                color: AppPalette.line,
                borderRadius: BorderRadius.circular(999),
              ),
            ),
            if (title != null) ...[
              Align(
                alignment: Alignment.centerLeft,
                child: Text(
                  title!,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.ink,
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.3,
                  ),
                ),
              ),
              const SizedBox(height: 14),
            ],
            child,
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// AppToast — floating dark pill (#161528), white text, leading status icon,
// auto-dismiss ~1.9s, rises from bottom (transform-only).
// ─────────────────────────────────────────────────────────────────────────────

enum AppToastKind { success, info, warning, error }

void showAppToast(
  BuildContext context, {
  required String message,
  AppToastKind kind = AppToastKind.success,
  Duration duration = const Duration(milliseconds: 1900),
}) {
  final overlay = Overlay.maybeOf(context, rootOverlay: true);
  if (overlay == null) return;

  late OverlayEntry entry;
  entry = OverlayEntry(
    builder: (ctx) => _ToastView(
      message: message,
      kind: kind,
      duration: duration,
      onClose: () => entry.remove(),
    ),
  );
  overlay.insert(entry);
}

class _ToastView extends StatefulWidget {
  const _ToastView({
    required this.message,
    required this.kind,
    required this.duration,
    required this.onClose,
  });

  final String message;
  final AppToastKind kind;
  final Duration duration;
  final VoidCallback onClose;

  @override
  State<_ToastView> createState() => _ToastViewState();
}

class _ToastViewState extends State<_ToastView>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 240),
  );

  @override
  void initState() {
    super.initState();
    _ctrl.forward();
    Future.delayed(widget.duration, () async {
      if (!mounted) return;
      await _ctrl.reverse();
      if (mounted) widget.onClose();
    });
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  IconData get _icon => switch (widget.kind) {
        AppToastKind.success => Icons.check_circle_rounded,
        AppToastKind.info => Icons.info_rounded,
        AppToastKind.warning => Icons.warning_amber_rounded,
        AppToastKind.error => Icons.error_rounded,
      };

  Color get _accent => switch (widget.kind) {
        AppToastKind.success => AppPalette.emeraldBright,
        AppToastKind.info => AppPalette.sky,
        AppToastKind.warning => AppPalette.amber,
        AppToastKind.error => AppPalette.rose,
      };

  @override
  Widget build(BuildContext context) {
    final reduce = MediaQuery.disableAnimationsOf(context);
    return Positioned(
      left: 16,
      right: 16,
      bottom: MediaQuery.of(context).padding.bottom + 28,
      child: IgnorePointer(
        child: AnimatedBuilder(
          animation: _ctrl,
          builder: (_, child) {
            final dy = reduce ? 0.0 : (1 - _ctrl.value) * 18;
            return Transform.translate(offset: Offset(0, dy), child: child);
          },
          child: Center(
            child: Container(
              padding:
                  const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: AppPalette.ink,
                borderRadius: BorderRadius.circular(999),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x33000000),
                    blurRadius: 28,
                    offset: Offset(0, 12),
                  ),
                ],
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(_icon, size: 18, color: _accent),
                  const SizedBox(width: 10),
                  Flexible(
                    child: Text(
                      widget.message,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.plusJakartaSans(
                        color: Colors.white,
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// AppBottomNav — glass bar (h70, m14, r26, blur20, translucent white, soft
// border + shadow). 4 labeled tab items + a centre raised gradient FAB
// (58×58, r20, grad-brand, shadow-brand, 4px bg-coloured border, offset −26).
//
// 5 items in the IA, but the centre is the FAB so we render 2 left + FAB +
// 2 right on the bar. `items.length` must be 5 with the FAB at index 2.
// ─────────────────────────────────────────────────────────────────────────────

class AppNavItem {
  const AppNavItem({required this.icon, required this.label});
  final IconData icon;
  final String label;
}

class AppBottomNav extends StatelessWidget {
  const AppBottomNav({
    super.key,
    required this.items,
    required this.currentIndex,
    required this.onTap,
  })  : assert(items.length == 5, 'AppBottomNav expects exactly 5 items '
            '(with index 2 rendered as the centre FAB).');

  final List<AppNavItem> items;
  final int currentIndex;
  final ValueChanged<int> onTap;

  @override
  Widget build(BuildContext context) {
    final s = context.semantics;
    final fabItem = items[2];
    final tabs = [items[0], items[1], items[3], items[4]];
    final tabIndices = [0, 1, 3, 4];

    return Padding(
      padding: const EdgeInsets.fromLTRB(14, 0, 14, 14),
      child: SizedBox(
        height: 70 + 26, // bar + FAB lift
        child: Stack(
          clipBehavior: Clip.none,
          alignment: Alignment.bottomCenter,
          children: [
            // Glass bar.
            ClipRRect(
              borderRadius: BorderRadius.circular(26),
              child: BackdropFilter(
                filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
                child: Container(
                  height: 70,
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.78),
                    borderRadius: BorderRadius.circular(26),
                    border: Border.all(
                      color: Colors.white.withValues(alpha: 0.7),
                      width: 1,
                    ),
                    boxShadow: s.shadow,
                  ),
                  child: Row(
                    children: [
                      Expanded(child: _tab(context, tabs[0], tabIndices[0])),
                      Expanded(child: _tab(context, tabs[1], tabIndices[1])),
                      const SizedBox(width: 72), // FAB slot
                      Expanded(child: _tab(context, tabs[2], tabIndices[2])),
                      Expanded(child: _tab(context, tabs[3], tabIndices[3])),
                    ],
                  ),
                ),
              ),
            ),
            // Centre raised FAB.
            Positioned(
              bottom: 70 - 26, // lift up −26 from the bar
              child: PressScale(
                onTap: () => onTap(2),
                child: Container(
                  width: 58,
                  height: 58,
                  decoration: BoxDecoration(
                    gradient: s.brandGradient,
                    borderRadius: BorderRadius.circular(20),
                    boxShadow: s.shadowBrand,
                    border: Border.all(
                      color: Theme.of(context).scaffoldBackgroundColor,
                      width: 4,
                    ),
                  ),
                  alignment: Alignment.center,
                  child: Icon(fabItem.icon, color: Colors.white, size: 26),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _tab(BuildContext context, AppNavItem item, int idx) {
    final active = currentIndex == idx;
    final color = active ? AppPalette.indigo : AppPalette.faint;
    return PressScale(
      onTap: () => onTap(idx),
      child: SizedBox(
        height: 70,
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(item.icon, color: color, size: 24),
            const SizedBox(height: 4),
            Text(
              item.label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: GoogleFonts.plusJakartaSans(
                color: color,
                fontSize: 10.5,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.1,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Entrance animation — transform-only Y rise (16 → 0) over ~450ms, staggered.
// Wrap a column of children with `AppStaggerColumn` or use `AppRiseIn` directly.
// ─────────────────────────────────────────────────────────────────────────────

class AppRiseIn extends StatefulWidget {
  const AppRiseIn({
    super.key,
    required this.child,
    this.delay = Duration.zero,
    this.distance = 16,
    this.duration = const Duration(milliseconds: 450),
  });

  final Widget child;
  final Duration delay;
  final double distance;
  final Duration duration;

  @override
  State<AppRiseIn> createState() => _AppRiseInState();
}

class _AppRiseInState extends State<AppRiseIn>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl =
      AnimationController(vsync: this, duration: widget.duration);

  @override
  void initState() {
    super.initState();
    Future.delayed(widget.delay, () {
      if (mounted) _ctrl.forward();
    });
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final reduce = MediaQuery.disableAnimationsOf(context);
    if (reduce) return widget.child;
    return AnimatedBuilder(
      animation: _ctrl,
      builder: (_, child) {
        final t = Curves.easeOutCubic.transform(_ctrl.value);
        return Transform.translate(
          offset: Offset(0, (1 - t) * widget.distance),
          child: child,
        );
      },
      child: widget.child,
    );
  }
}

class AppStaggerColumn extends StatelessWidget {
  const AppStaggerColumn({
    super.key,
    required this.children,
    this.spacing = 14,
    this.stagger = const Duration(milliseconds: 60),
    this.crossAxisAlignment = CrossAxisAlignment.stretch,
  });

  final List<Widget> children;
  final double spacing;
  final Duration stagger;
  final CrossAxisAlignment crossAxisAlignment;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: crossAxisAlignment,
      children: [
        for (var i = 0; i < children.length; i++) ...[
          if (i > 0) SizedBox(height: spacing),
          AppRiseIn(delay: stagger * i, child: children[i]),
        ],
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// SectionEyebrow — UPPERCASE 12/800, letter-spacing ~1.
// ─────────────────────────────────────────────────────────────────────────────

class SectionEyebrow extends StatelessWidget {
  const SectionEyebrow(this.text, {super.key, this.trailing});

  final String text;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final label = Text(
      text.toUpperCase(),
      style: GoogleFonts.plusJakartaSans(
        color: AppPalette.muted,
        fontSize: 12,
        fontWeight: FontWeight.w800,
        letterSpacing: 1.0,
      ),
    );
    if (trailing == null) return label;
    return Row(
      children: [
        Expanded(child: label),
        trailing!,
      ],
    );
  }
}
