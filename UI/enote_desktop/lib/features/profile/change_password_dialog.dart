import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../widgets/entity_form_scaffold.dart';
import 'profile_provider.dart';

class ChangePasswordDialog extends StatefulWidget {
  const ChangePasswordDialog({super.key});

  @override
  State<ChangePasswordDialog> createState() => _ChangePasswordDialogState();
}

class _ChangePasswordDialogState extends State<ChangePasswordDialog> {
  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  @override
  void dispose() {
    _currentPasswordController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final authState = context.read<AuthState>();
    final profileProvider = context.read<ProfileProvider>();
    await profileProvider.changePassword(
      ChangePasswordRequest(
        currentPassword: _currentPasswordController.text,
        newPassword: _newPasswordController.text,
        confirmNewPassword: _confirmPasswordController.text,
      ),
    );
    if (!mounted) return false;
    navigator.popUntil((route) => route.isFirst);
    authState.logout();
    messenger.showSnackBar(
      const SnackBar(
        content: Text('Lozinka je promijenjena. Prijavite se ponovo.'),
      ),
    );
    return false;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: 'Promijeni lozinku',
      presentation: EntityFormPresentation.dialog,
      onSave: _save,
      fieldsBuilder: (context) => [
        TextFormField(
          controller: _currentPasswordController,
          decoration: const InputDecoration(labelText: 'Trenutna lozinka'),
          obscureText: true,
          validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _newPasswordController,
          decoration: const InputDecoration(labelText: 'Nova lozinka'),
          obscureText: true,
          validator: Validators.password,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _confirmPasswordController,
          decoration: const InputDecoration(labelText: 'Potvrdi novu lozinku'),
          obscureText: true,
          validator:
              Validators.confirmPassword(() => _newPasswordController.text),
        ),
      ],
    );
  }
}
