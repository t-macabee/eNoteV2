import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/app_text_field.dart';
import '../../widgets/form_submit_state.dart';
import '../../widgets/mobile_form_scaffold.dart';
import 'auth_provider.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen>
    with FormSubmitState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();

  @override
  void dispose() {
    _usernameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmController.dispose();
    _firstNameController.dispose();
    _lastNameController.dispose();
    super.dispose();
  }

  String _text(TextEditingController controller) => controller.text.trim();

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }
    await submit(() async {
      final username = _usernameController.text.trim();
      final password = _passwordController.text;
      final firstName = _text(_firstNameController);
      final lastName = _text(_lastNameController);
      await context.read<AuthProvider>().register(
        RegisterRequest(
          username: username,
          email: _emailController.text.trim(),
          password: password,
          firstName: firstName.isEmpty ? null : firstName,
          lastName: lastName.isEmpty ? null : lastName,
        ),
      );
      if (!mounted) return;
      await context.read<AuthState>().login(username, password);
    });
  }

  @override
  Widget build(BuildContext context) {
    return MobileFormScaffold(
      title: 'Registracija',
      submitLabel: 'Registruj se',
      onSubmit: _submit,
      isBusy: isSubmitting,
      errorMessage: submitError,
      children: [
        Form(
          key: _formKey,
          child: Column(
            children: [
              AppTextField(
                controller: _usernameController,
                label: 'Korisničko ime *',
                validator: Validators.required('Korisničko ime'),
              ),
              const SizedBox(height: 16),
              AppTextField(
                controller: _emailController,
                label: 'Email *',
                keyboardType: TextInputType.emailAddress,
                validator: Validators.email,
              ),
              const SizedBox(height: 16),
              AppTextField(
                controller: _passwordController,
                label: 'Lozinka *',
                obscure: true,
                validator: Validators.password,
              ),
              const SizedBox(height: 16),
              AppTextField(
                controller: _confirmController,
                label: 'Potvrda lozinke *',
                obscure: true,
                validator:
                    Validators.confirmPassword(() => _passwordController.text),
              ),
              const SizedBox(height: 16),
              AppTextField(controller: _firstNameController, label: 'Ime'),
              const SizedBox(height: 16),
              AppTextField(
                controller: _lastNameController,
                label: 'Prezime',
                textInputAction: TextInputAction.done,
                onFieldSubmitted: (_) => _submit(),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
