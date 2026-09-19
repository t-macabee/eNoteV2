import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/form_submit_state.dart';
import '../../widgets/mobile_form_scaffold.dart';
import 'auth_provider.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen>
    with FormSubmitState<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  bool _sent = false;

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }
    await submit(() async {
      await context.read<AuthProvider>().forgotPassword(
        _emailController.text.trim(),
      );
      if (mounted) {
        setState(() => _sent = true);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_sent) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Zaboravljena lozinka'),
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
              'Ako postoji račun s ovom adresom, poslali smo link za reset lozinke.',
            ),
            const SizedBox(height: 16),
            OutlinedButton(
              onPressed: () => Navigator.of(context).maybePop(),
              child: const Text('Nazad na prijavu'),
            ),
          ],
        ),
      );
    }
    return MobileFormScaffold(
      title: 'Zaboravljena lozinka',
      submitLabel: 'Pošalji link',
      onSubmit: _submit,
      isBusy: isSubmitting,
      errorMessage: submitError,
      children: [
        Form(
          key: _formKey,
          child: TextFormField(
            controller: _emailController,
            decoration: const InputDecoration(labelText: 'Email *'),
            keyboardType: TextInputType.emailAddress,
            textInputAction: TextInputAction.done,
            onFieldSubmitted: (_) => _submit(),
            validator: Validators.email,
          ),
        ),
      ],
    );
  }
}
