import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/app_text_field.dart';
import '../../widgets/form_submit_state.dart';
import '../../widgets/mobile_form_scaffold.dart';
import 'auth_provider.dart';

class ResetPasswordScreen extends StatefulWidget {
  final String? email;
  final String? token;

  const ResetPasswordScreen({super.key, this.email, this.token});

  @override
  State<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends State<ResetPasswordScreen>
    with FormSubmitState<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _emailController;
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();

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
    await submit(() async {
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
    });
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
      isBusy: isSubmitting,
      errorMessage: submitError,
      children: [
        Form(
          key: _formKey,
          child: Column(
            children: [
              AppTextField(
                controller: _emailController,
                label: 'Email',
                readOnly: true,
              ),
              const SizedBox(height: 16),
              AppTextField(
                controller: _passwordController,
                label: 'Nova lozinka *',
                obscure: true,
                validator: Validators.password,
              ),
              const SizedBox(height: 16),
              AppTextField(
                controller: _confirmController,
                label: 'Potvrda nove lozinke *',
                obscure: true,
                textInputAction: TextInputAction.done,
                onFieldSubmitted: (_) => _submit(),
                validator:
                    Validators.confirmPassword(() => _passwordController.text),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
