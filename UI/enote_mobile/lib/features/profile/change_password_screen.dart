import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/app_text_field.dart';
import '../../widgets/form_submit_state.dart';
import '../../widgets/mobile_form_scaffold.dart';
import 'profile_provider.dart';

class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen>
    with FormSubmitState<ChangePasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _currentController = TextEditingController();
  final _newController = TextEditingController();
  final _confirmController = TextEditingController();

  @override
  void dispose() {
    _currentController.dispose();
    _newController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final authState = context.read<AuthState>();
    final provider = context.read<ProfileProvider>();
    await submit(() async {
      await provider.changePassword(
        ChangePasswordRequest(
          currentPassword: _currentController.text,
          newPassword: _newController.text,
          confirmNewPassword: _confirmController.text,
        ),
      );
      if (!mounted) return;
      navigator.popUntil((route) => route.isFirst);
      authState.logout();
      messenger.showSnackBar(
        const SnackBar(
          content: Text('Lozinka je promijenjena. Prijavite se ponovo.'),
        ),
      );
    });
  }

  @override
  Widget build(BuildContext context) {
    return MobileFormScaffold(
      title: 'Promjena lozinke',
      submitLabel: 'Promijeni lozinku',
      onSubmit: _submit,
      isBusy: isSubmitting,
      errorMessage: submitError,
      children: [
        Form(
          key: _formKey,
          autovalidateMode: AutovalidateMode.onUserInteraction,
          child: Column(
            children: [
              AppTextField(
                controller: _currentController,
                label: 'Trenutna lozinka *',
                obscure: true,
                validator: Validators.required('Trenutna lozinka'),
              ),
              const SizedBox(height: AppSpacing.lg),
              AppTextField(
                controller: _newController,
                label: 'Nova lozinka *',
                obscure: true,
                validator: Validators.password,
              ),
              const SizedBox(height: AppSpacing.lg),
              AppTextField(
                controller: _confirmController,
                label: 'Potvrdi novu lozinku *',
                obscure: true,
                textInputAction: TextInputAction.done,
                validator: Validators.confirmPassword(
                  () => _newController.text,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
