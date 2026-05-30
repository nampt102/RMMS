import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/auth_repository.dart';
import '../auth_error_text.dart';

class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() =>
      _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailCtrl = TextEditingController();
  bool _loading = false;
  bool _sent = false;
  String? _errorText;

  @override
  void dispose() {
    _emailCtrl.dispose();
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
      await ref.read(authRepositoryProvider).forgotPassword(_emailCtrl.text.trim());
      if (mounted) setState(() => _sent = true);
    } on ApiException catch (e) {
      // The endpoint is neutral by design; only transport errors surface here.
      if (mounted) setState(() => _errorText = authErrorText(l, e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l.forgotTitle)),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: _sent ? _buildSent(l, context) : _buildForm(l, context),
        ),
      ),
    );
  }

  Widget _buildForm(AppLocalizations l, BuildContext context) {
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SizedBox(height: 16),
          Text(l.forgotIntro),
          const SizedBox(height: 24),
          TextFormField(
            controller: _emailCtrl,
            keyboardType: TextInputType.emailAddress,
            textInputAction: TextInputAction.done,
            enabled: !_loading,
            decoration: InputDecoration(labelText: l.forgotEmailLabel),
            validator: (v) =>
                (v == null || !v.contains('@')) ? l.loginEmailInvalid : null,
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
                : Text(l.forgotSubmit),
          ),
        ],
      ),
    );
  }

  Widget _buildSent(AppLocalizations l, BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SizedBox(height: 24),
        Icon(
          Icons.mark_email_read_outlined,
          size: 64,
          color: Theme.of(context).colorScheme.primary,
        ),
        const SizedBox(height: 16),
        Text(
          l.forgotSentTitle,
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: 8),
        Text(l.forgotSentBody, textAlign: TextAlign.center),
        const SizedBox(height: 24),
        OutlinedButton(
          onPressed: () => context.go(AppRoutes.resetPassword),
          child: Text(l.forgotEnterCode),
        ),
        const SizedBox(height: 12),
        TextButton(
          onPressed: () => context.go(AppRoutes.login),
          child: Text(l.forgotBackToLogin),
        ),
      ],
    );
  }
}
