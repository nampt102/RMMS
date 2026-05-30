import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/auth_repository.dart';
import '../auth_error_text.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _fullNameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();
  bool _loading = false;
  String? _errorText;

  @override
  void dispose() {
    _fullNameCtrl.dispose();
    _emailCtrl.dispose();
    _phoneCtrl.dispose();
    _passwordCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  /// Mirrors the backend rule: ≥8 chars + at least 1 letter + 1 digit.
  String? _validatePassword(AppLocalizations l, String? v) {
    if (v == null || v.length < 8) return l.loginPasswordTooShort;
    final hasLetter = v.contains(RegExp('[A-Za-z]'));
    final hasDigit = v.contains(RegExp('[0-9]'));
    if (!hasLetter || !hasDigit) return l.registerPasswordRule;
    return null;
  }

  Future<void> _submit() async {
    final l = AppLocalizations.of(context);
    if (!(_formKey.currentState?.validate() ?? false)) return;

    // PG registers in the language the app is currently running in.
    final lang = Localizations.localeOf(context).languageCode == 'en' ? 'en' : 'vi';

    setState(() {
      _loading = true;
      _errorText = null;
    });

    try {
      await ref.read(authRepositoryProvider).register(
            email: _emailCtrl.text.trim(),
            password: _passwordCtrl.text,
            fullName: _fullNameCtrl.text.trim(),
            phone: _phoneCtrl.text.trim(),
            preferredLanguage: lang,
          );
      if (!mounted) return;
      // Hand the email to the verify screen so it can show whom to verify.
      context.go('${AppRoutes.verifyEmail}?email=${Uri.encodeQueryComponent(_emailCtrl.text.trim())}');
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

    return Scaffold(
      appBar: AppBar(title: Text(l.registerTitle)),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: ListView(
              children: [
                const SizedBox(height: 8),
                Text(l.registerIntro),
                const SizedBox(height: 24),
                TextFormField(
                  controller: _fullNameCtrl,
                  textInputAction: TextInputAction.next,
                  enabled: !_loading,
                  decoration: InputDecoration(labelText: l.registerFullNameLabel),
                  validator: (v) => (v == null || v.trim().isEmpty)
                      ? l.registerFullNameRequired
                      : null,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _emailCtrl,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  enabled: !_loading,
                  decoration: InputDecoration(labelText: l.loginEmailLabel),
                  validator: (v) =>
                      (v == null || !v.contains('@')) ? l.loginEmailInvalid : null,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _phoneCtrl,
                  keyboardType: TextInputType.phone,
                  textInputAction: TextInputAction.next,
                  enabled: !_loading,
                  decoration: InputDecoration(
                    labelText: l.registerPhoneLabel,
                    helperText: l.registerPhoneOptional,
                  ),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _passwordCtrl,
                  obscureText: true,
                  textInputAction: TextInputAction.next,
                  enabled: !_loading,
                  decoration: InputDecoration(
                    labelText: l.loginPasswordLabel,
                    helperText: l.registerPasswordRule,
                  ),
                  validator: (v) => _validatePassword(l, v),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _confirmCtrl,
                  obscureText: true,
                  textInputAction: TextInputAction.done,
                  enabled: !_loading,
                  decoration: InputDecoration(labelText: l.resetConfirmLabel),
                  validator: (v) =>
                      (v != _passwordCtrl.text) ? l.resetPasswordMismatch : null,
                  onFieldSubmitted: (_) => _submit(),
                ),
                if (_errorText != null) ...[
                  const SizedBox(height: 16),
                  Text(
                    _errorText!,
                    style: TextStyle(color: Theme.of(context).colorScheme.error),
                  ),
                ],
                const SizedBox(height: 24),
                FilledButton(
                  onPressed: _loading ? null : _submit,
                  child: _loading
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : Text(l.registerSubmit),
                ),
                const SizedBox(height: 12),
                TextButton(
                  onPressed: _loading ? null : () => context.go(AppRoutes.login),
                  child: Text(l.registerHaveAccount),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
