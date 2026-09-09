import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../shell/app_router.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _busy = false;
  bool _obscured = true;
  bool _sessionExpired = false;
  String? _errorMessage;

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
    setState(() {
      _busy = true;
      _errorMessage = null;
    });
    try {
      await context.read<AuthState>().login(
        _usernameController.text.trim(),
        _passwordController.text,
      );
    } catch (e) {
      if (mounted) {
        setState(() => _errorMessage = userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
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
            TextFormField(
              controller: _usernameController,
              decoration: const InputDecoration(labelText: 'Korisničko ime'),
              textInputAction: TextInputAction.next,
              validator: Validators.required('Korisničko ime'),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _passwordController,
              decoration: InputDecoration(
                labelText: 'Lozinka',
                suffixIcon: IconButton(
                  icon: Icon(
                    _obscured ? Icons.visibility : Icons.visibility_off,
                  ),
                  onPressed: () => setState(() => _obscured = !_obscured),
                ),
              ),
              obscureText: _obscured,
              textInputAction: TextInputAction.done,
              onFieldSubmitted: (_) => _submit(),
              validator: Validators.required('Lozinka'),
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _busy ? null : _submit,
              child: _busy
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Prijava'),
            ),
            if (_errorMessage != null) ...[
              const SizedBox(height: 12),
              ErrorBanner(message: _errorMessage!),
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
