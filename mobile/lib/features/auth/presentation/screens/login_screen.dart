import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../application/auth_controller.dart';
import '../auth_error_text.dart';

/// Redesign 2026 — Login.
///
/// Visual: brand-tile logo + display title "Chào mừng / trở lại 👋", subtitle,
/// two pill inputs (height 58, radius 17, white, shadow-sm) with leading icons
/// and password eye toggle, right-aligned "Quên mật khẩu?" link, primary
/// gradient button, and a footer link to register.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  bool _loading = false;
  bool _obscure = true;
  String? _errorText;

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passwordCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final l = AppLocalizations.of(context);
    if (!(_formKey.currentState?.validate() ?? false)) return;

    setState(() {
      _loading = true;
      _errorText = null;
    });

    try {
      await ref.read(authControllerProvider.notifier).login(
            email: _emailCtrl.text.trim(),
            password: _passwordCtrl.text,
          );
    } on ApiException catch (e) {
      if (mounted) setState(() => _errorText = authErrorText(l, e));
    } catch (_) {
      if (mounted) setState(() => _errorText = l.errorGeneric);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final s = context.semantics;

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 24, 20, 24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 8),
                // Brand tile (64×64 grad-brand, radius 22).
                Align(
                  alignment: Alignment.centerLeft,
                  child: SizedBox(
                    width: 64,
                    height: 64,
                    child: DecoratedBox(
                      decoration: BoxDecoration(
                        gradient: s.brandGradient,
                        borderRadius: BorderRadius.circular(22),
                        boxShadow: s.shadowBrand,
                      ),
                      child: const Icon(Icons.store_rounded,
                          color: Colors.white, size: 30),
                    ),
                  ),
                ),
                const SizedBox(height: 22),
                // Display title — two lines, 34/800, negative tracking.
                Text(
                  '${l.loginWelcomeLine1}\n${l.loginWelcomeLine2}',
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.ink,
                    fontSize: 34,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -1.0,
                    height: 1.05,
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  l.loginSubtitle,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.muted,
                    fontSize: 14.5,
                    fontWeight: FontWeight.w500,
                    height: 1.45,
                  ),
                ),
                const SizedBox(height: 28),
                // Email pill input.
                _PillField(
                  controller: _emailCtrl,
                  hint: l.loginEmailLabel,
                  icon: Icons.mail_rounded,
                  enabled: !_loading,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  autofillHints: const [
                    AutofillHints.username,
                    AutofillHints.email,
                  ],
                  validator: (v) =>
                      (v == null || !v.contains('@')) ? l.loginEmailInvalid : null,
                ),
                const SizedBox(height: 14),
                // Password pill input + eye toggle.
                _PillField(
                  controller: _passwordCtrl,
                  hint: l.loginPasswordLabel,
                  icon: Icons.lock_rounded,
                  enabled: !_loading,
                  obscureText: _obscure,
                  textInputAction: TextInputAction.done,
                  autofillHints: const [AutofillHints.password],
                  trailing: GestureDetector(
                    onTap: () => setState(() => _obscure = !_obscure),
                    child: Icon(
                      _obscure
                          ? Icons.visibility_off_rounded
                          : Icons.visibility_rounded,
                      color: AppPalette.faint,
                      size: 20,
                    ),
                  ),
                  validator: (v) =>
                      (v == null || v.length < 8) ? l.loginPasswordTooShort : null,
                  onFieldSubmitted: (_) => _submit(),
                ),
                const SizedBox(height: 12),
                // Quên mật khẩu? — right aligned, indigo, 14/700.
                Align(
                  alignment: Alignment.centerRight,
                  child: PressScale(
                    onTap: _loading
                        ? null
                        : () => context.push(AppRoutes.forgotPassword),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 6, vertical: 4),
                      child: Text(
                        l.loginForgot,
                        style: GoogleFonts.plusJakartaSans(
                          color: AppPalette.indigo,
                          fontSize: 14,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ),
                ),
                if (_errorText != null) ...[
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 14, vertical: 12),
                    decoration: BoxDecoration(
                      color: AppPalette.chipRoseBg,
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.error_rounded,
                            color: AppPalette.rose, size: 18),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            _errorText!,
                            style: GoogleFonts.plusJakartaSans(
                              color: AppPalette.chipRoseFg,
                              fontSize: 13.5,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                const SizedBox(height: 18),
                AppButton.primary(
                  label: l.loginSubmit,
                  loading: _loading,
                  onPressed: _submit,
                ),
                const SizedBox(height: 18),
                // Footer: "Chưa có tài khoản? Đăng ký".
                Center(
                  child: PressScale(
                    onTap: _loading
                        ? null
                        : () => context.push(AppRoutes.register),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 6),
                      child: Text(
                        l.loginRegister,
                        style: GoogleFonts.plusJakartaSans(
                          color: AppPalette.muted,
                          fontSize: 14,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
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

/// White pill input — height 58, radius 17, shadow-sm, leading icon (20px),
/// optional trailing widget. Wraps a [TextFormField] with a transparent
/// decoration so we control the surface.
class _PillField extends StatelessWidget {
  const _PillField({
    required this.controller,
    required this.hint,
    required this.icon,
    this.trailing,
    this.enabled = true,
    this.obscureText = false,
    this.keyboardType,
    this.textInputAction,
    this.autofillHints,
    this.validator,
    this.onFieldSubmitted,
  });

  final TextEditingController controller;
  final String hint;
  final IconData icon;
  final Widget? trailing;
  final bool enabled;
  final bool obscureText;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final Iterable<String>? autofillHints;
  final String? Function(String?)? validator;
  final ValueChanged<String>? onFieldSubmitted;

  @override
  Widget build(BuildContext context) {
    final s = context.semantics;
    return Container(
      decoration: BoxDecoration(
        color: AppPalette.surface,
        borderRadius: BorderRadius.circular(17),
        boxShadow: s.shadowSm,
      ),
      child: TextFormField(
        controller: controller,
        enabled: enabled,
        obscureText: obscureText,
        keyboardType: keyboardType,
        textInputAction: textInputAction,
        autofillHints: autofillHints,
        validator: validator,
        onFieldSubmitted: onFieldSubmitted,
        style: GoogleFonts.plusJakartaSans(
          color: AppPalette.ink,
          fontSize: 15,
          fontWeight: FontWeight.w600,
        ),
        decoration: InputDecoration(
          isCollapsed: false,
          filled: true,
          fillColor: Colors.transparent,
          hintText: hint,
          hintStyle: GoogleFonts.plusJakartaSans(
            color: AppPalette.faint,
            fontSize: 15,
            fontWeight: FontWeight.w500,
          ),
          contentPadding: const EdgeInsets.symmetric(vertical: 18),
          prefixIcon: Padding(
            padding: const EdgeInsets.only(left: 16, right: 12),
            child: Icon(icon, color: AppPalette.indigo, size: 20),
          ),
          prefixIconConstraints:
              const BoxConstraints(minWidth: 0, minHeight: 0),
          suffixIcon: trailing == null
              ? null
              : Padding(
                  padding: const EdgeInsets.only(right: 14, left: 8),
                  child: trailing,
                ),
          suffixIconConstraints:
              const BoxConstraints(minWidth: 0, minHeight: 0),
          border: const OutlineInputBorder(
            borderRadius: BorderRadius.all(Radius.circular(17)),
            borderSide: BorderSide.none,
          ),
          enabledBorder: const OutlineInputBorder(
            borderRadius: BorderRadius.all(Radius.circular(17)),
            borderSide: BorderSide.none,
          ),
          focusedBorder: const OutlineInputBorder(
            borderRadius: BorderRadius.all(Radius.circular(17)),
            borderSide: BorderSide(color: AppPalette.indigo, width: 1.6),
          ),
          errorBorder: const OutlineInputBorder(
            borderRadius: BorderRadius.all(Radius.circular(17)),
            borderSide: BorderSide(color: AppPalette.rose, width: 1.2),
          ),
          focusedErrorBorder: const OutlineInputBorder(
            borderRadius: BorderRadius.all(Radius.circular(17)),
            borderSide: BorderSide(color: AppPalette.rose, width: 1.6),
          ),
        ),
      ),
    );
  }
}
