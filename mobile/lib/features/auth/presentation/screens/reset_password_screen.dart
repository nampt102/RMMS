import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/auth_repository.dart';
import '../auth_error_text.dart';

class ResetPasswordScreen extends ConsumerStatefulWidget {
  const ResetPasswordScreen({super.key, this.initialToken});

  /// Token delivered via the `rmms://reset-password?token=...` deep link.
  /// When null/empty the user can paste the code manually (sprint-01 R-4).
  final String? initialToken;

  @override
  ConsumerState<ResetPasswordScreen> createState() =>
      _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends ConsumerState<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _tokenCtrl;
  final _passwordCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();
  bool _loading = false;
  String? _errorText;

  @override
  void initState() {
    super.initState();
    _tokenCtrl = TextEditingController(text: widget.initialToken ?? '');
  }

  @override
  void dispose() {
    _tokenCtrl.dispose();
    _passwordCtrl.dispose();
    _confirmCtrl.dispose();
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
      await ref.read(authRepositoryProvider).resetPassword(
            token: _tokenCtrl.text.trim(),
            newPassword: _passwordCtrl.text,
          );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(l.resetSuccess)),
      );
      context.go(AppRoutes.login);
    } on ApiException catch (e) {
      if (mounted) setState(() => _errorText = authErrorText(l, e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l.resetTitle)),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: ListView(
              children: [
                const SizedBox(height: 8),
                Text(l.resetIntro),
                const SizedBox(height: 24),
                TextFormField(
                  controller: _tokenCtrl,
                  enabled: !_loading,
                  decoration: InputDecoration(labelText: l.resetTokenLabel),
                  validator: (v) =>
                      (v == null || v.trim().isEmpty) ? l.resetTokenRequired : null,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _passwordCtrl,
                  obscureText: true,
                  enabled: !_loading,
                  decoration:
                      InputDecoration(labelText: l.resetNewPasswordLabel),
                  validator: (v) =>
                      (v == null || v.length < 8) ? l.loginPasswordTooShort : null,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _confirmCtrl,
                  obscureText: true,
                  enabled: !_loading,
                  textInputAction: TextInputAction.done,
                  decoration: InputDecoration(labelText: l.resetConfirmLabel),
                  validator: (v) =>
                      (v != _passwordCtrl.text) ? l.resetPasswordMismatch : null,
                  onFieldSubmitted: (_) => _submit(),
                ),
                if (_errorText != null) ...[
                  const SizedBox(height: 16),
                  Text(
                    _errorText!,
                    style:
                        TextStyle(color: Theme.of(context).colorScheme.error),
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
                      : Text(l.resetSubmit),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
