import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/mobile_form_scaffold.dart';
import 'auth_provider.dart';

class ResetPasswordScreen extends StatefulWidget {
  final String? email;
  final String? token;

  const ResetPasswordScreen({super.key, this.email, this.token});

  @override
  State<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends State<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _emailController;
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();
  bool _busy = false;
  bool _obscured = true;
  bool _confirmObscured = true;
  String? _errorMessage;

  bool get _hasParams =>
      widget.email != null &&
      widget.email!.isNotEmpty &&
      widget.token != null &&
      widget.token!.isNotEmpty;

  @override
  void initState() {
    super.initState();
    _emailController = TextEditingController(text: widget.email ?? '');
  }

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _confirmController.dispose();
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
      await context.read<AuthProvider>().resetPassword(
        email: widget.email!,
        token: widget.token!,
        newPassword: _passwordController.text,
      );
      if (!mounted) return;
      await context.read<AuthState>().logout();
      if (!mounted) return;
      Navigator.of(context).popUntil((route) => route.isFirst);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Lozinka je uspješno promijenjena. Prijavite se novom lozinkom.',
          ),
        ),
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
    if (!_hasParams) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Reset lozinke'),
          actions: [
            IconButton(
              icon: const Icon(Icons.close),
              onPressed: () => Navigator.of(context).maybePop(),
            ),
          ],
        ),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            const Text(
              'Link za reset lozinke nije ispravan. Zatražite novi.',
            ),
            const SizedBox(height: 16),
            OutlinedButton(
              onPressed: () => Navigator.of(
                context,
              ).pushReplacementNamed(AppRouter.forgotPassword),
              child: const Text('Zatraži novi link'),
            ),
          ],
        ),
      );
    }
    return MobileFormScaffold(
      title: 'Reset lozinke',
      submitLabel: 'Promijeni lozinku',
      onSubmit: _submit,
      isBusy: _busy,
      errorMessage: _errorMessage,
      children: [
        Form(
          key: _formKey,
          child: Column(
            children: [
              TextFormField(
                controller: _emailController,
                decoration: const InputDecoration(labelText: 'Email'),
                readOnly: true,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _passwordController,
                decoration: InputDecoration(
                  labelText: 'Nova lozinka *',
                  suffixIcon: IconButton(
                    icon: Icon(
                      _obscured ? Icons.visibility : Icons.visibility_off,
                    ),
                    onPressed: () => setState(() => _obscured = !_obscured),
                  ),
                ),
                obscureText: _obscured,
                textInputAction: TextInputAction.next,
                validator: Validators.password,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _confirmController,
                decoration: InputDecoration(
                  labelText: 'Potvrda nove lozinke *',
                  suffixIcon: IconButton(
                    icon: Icon(
                      _confirmObscured
                          ? Icons.visibility
                          : Icons.visibility_off,
                    ),
                    onPressed: () => setState(
                      () => _confirmObscured = !_confirmObscured,
                    ),
                  ),
                ),
                obscureText: _confirmObscured,
                textInputAction: TextInputAction.done,
                onFieldSubmitted: (_) => _submit(),
                validator: (value) => Validators.confirmPassword(
                  _passwordController.text,
                )(value),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
