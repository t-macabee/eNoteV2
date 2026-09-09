import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/mobile_form_scaffold.dart';
import 'auth_provider.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  bool _busy = false;
  bool _obscured = true;
  bool _confirmObscured = true;
  String? _errorMessage;

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

  String _text(TextEditingController controller) {
    final value = controller.text.trim();
    return value.isEmpty ? '' : value;
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
    return MobileFormScaffold(
      title: 'Registracija',
      submitLabel: 'Registruj se',
      onSubmit: _submit,
      isBusy: _busy,
      errorMessage: _errorMessage,
      children: [
        Form(
          key: _formKey,
          child: Column(
            children: [
              TextFormField(
                controller: _usernameController,
                decoration: const InputDecoration(
                  labelText: 'Korisničko ime *',
                ),
                textInputAction: TextInputAction.next,
                validator: Validators.required('Korisničko ime'),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _emailController,
                decoration: const InputDecoration(labelText: 'Email *'),
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.next,
                validator: Validators.email,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _passwordController,
                decoration: InputDecoration(
                  labelText: 'Lozinka *',
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
                  labelText: 'Potvrda lozinke *',
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
                textInputAction: TextInputAction.next,
                validator: (value) => Validators.confirmPassword(
                  _passwordController.text,
                )(value),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _firstNameController,
                decoration: const InputDecoration(labelText: 'Ime'),
                textInputAction: TextInputAction.next,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _lastNameController,
                decoration: const InputDecoration(labelText: 'Prezime'),
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
