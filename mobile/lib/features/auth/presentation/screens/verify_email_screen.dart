import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/auth_repository.dart';
import '../auth_error_text.dart';

class VerifyEmailScreen extends ConsumerStatefulWidget {
  const VerifyEmailScreen({super.key, this.initialToken, this.email});

  /// Token from the `rmms://verify-email?token=...` deep link (auto-verified).
  final String? initialToken;

  /// Email passed from the register screen, shown for context.
  final String? email;

  @override
  ConsumerState<VerifyEmailScreen> createState() => _VerifyEmailScreenState();
}

class _VerifyEmailScreenState extends ConsumerState<VerifyEmailScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _tokenCtrl;
  bool _loading = false;
  bool _verified = false;
  String? _errorText;

  @override
  void initState() {
    super.initState();
    _tokenCtrl = TextEditingController(text: widget.initialToken ?? '');

    final token = widget.initialToken;
    if (token != null && token.trim().isNotEmpty) {
      // One-tap verification from the email deep link.
      WidgetsBinding.instance.addPostFrameCallback((_) => _verify(token.trim()));
    }
  }

  @override
  void dispose() {
    _tokenCtrl.dispose();
    super.dispose();
  }

  Future<void> _verify(String token) async {
    final l = AppLocalizations.of(context);
    setState(() {
      _loading = true;
      _errorText = null;
    });

    try {
      await ref.read(authRepositoryProvider).verifyEmail(token);
      if (mounted) setState(() => _verified = true);
    } on ApiException catch (e) {
      if (mounted) setState(() => _errorText = authErrorText(l, e));
    } catch (_) {
      if (mounted) setState(() => _errorText = l.errorGeneric);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _submitManual() {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    _verify(_tokenCtrl.text.trim());
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l.verifyTitle)),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: _verified ? _buildSuccess(l, context) : _buildForm(l, context),
        ),
      ),
    );
  }

  Widget _buildForm(AppLocalizations l, BuildContext context) {
    final email = widget.email;
    return Form(
      key: _formKey,
      child: ListView(
        children: [
          const SizedBox(height: 8),
          Text(email == null || email.isEmpty
              ? l.verifyIntro
              : l.verifyIntroWithEmail(email)),
          const SizedBox(height: 24),
          TextFormField(
            controller: _tokenCtrl,
            enabled: !_loading,
            textInputAction: TextInputAction.done,
            decoration: InputDecoration(labelText: l.verifyTokenLabel),
            validator: (v) =>
                (v == null || v.trim().isEmpty) ? l.verifyTokenRequired : null,
            onFieldSubmitted: (_) => _submitManual(),
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
            onPressed: _loading ? null : _submitManual,
            child: _loading
                ? const SizedBox(
                    height: 20,
                    width: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Text(l.verifySubmit),
          ),
          const SizedBox(height: 12),
          TextButton(
            onPressed: _loading ? null : () => context.go(AppRoutes.login),
            child: Text(l.forgotBackToLogin),
          ),
        ],
      ),
    );
  }

  Widget _buildSuccess(AppLocalizations l, BuildContext context) {
    return Column(
      mainAxisAlignment: MainAxisAlignment.center,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Icon(
          Icons.verified_outlined,
          size: 72,
          color: Theme.of(context).colorScheme.primary,
        ),
        const SizedBox(height: 16),
        Text(
          l.verifySuccessTitle,
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: 8),
        Text(l.verifySuccessBody, textAlign: TextAlign.center),
        const SizedBox(height: 32),
        FilledButton(
          onPressed: () => context.go(AppRoutes.login),
          child: Text(l.verifyGoToLogin),
        ),
      ],
    );
  }
}
