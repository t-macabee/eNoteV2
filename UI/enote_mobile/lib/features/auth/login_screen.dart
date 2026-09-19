import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../shell/app_router.dart';
import '../../widgets/app_text_field.dart';
import '../../widgets/form_submit_state.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen>
    with FormSubmitState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _sessionExpired = false;

  @override
  void initState() {
    super.initState();
    _sessionExpired = context.read<SessionController>().consumeSessionExpired();
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }
    await submit(
      () => context.read<AuthState>().login(
            _usernameController.text.trim(),
            _passwordController.text,
          ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Prijava')),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (_sessionExpired) ...[
              const ErrorBanner(
                message: 'Vaša sesija je istekla. Prijavite se ponovo.',
              ),
              const SizedBox(height: 16),
            ],
            AppTextField(
              controller: _usernameController,
              label: 'Korisničko ime',
              validator: Validators.required('Korisničko ime'),
            ),
            const SizedBox(height: 16),
            AppTextField(
              controller: _passwordController,
              label: 'Lozinka',
              obscure: true,
              textInputAction: TextInputAction.done,
              onFieldSubmitted: (_) => _submit(),
              validator: Validators.required('Lozinka'),
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: isSubmitting ? null : _submit,
              child: isSubmitting
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Prijava'),
            ),
            if (submitError != null) ...[
              const SizedBox(height: 12),
              ErrorBanner(message: submitError!),
            ],
            const SizedBox(height: 8),
            TextButton(
              onPressed: () =>
                  Navigator.of(context).pushNamed(AppRouter.register),
              child: const Text('Registruj se'),
            ),
            TextButton(
              onPressed: () =>
                  Navigator.of(context).pushNamed(AppRouter.forgotPassword),
              child: const Text('Zaboravljena lozinka?'),
            ),
          ],
        ),
      ),
    );
  }
}
