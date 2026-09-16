import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../widgets/entity_form_scaffold.dart';
import 'profile_provider.dart';

/// "Promijeni lozinku" form opened from [ProfileDialog] — its own
/// [EntityFormScaffold] with its own Save/Cancel. The backend rotates the
/// security stamp on a password change, so the current JWT is rejected from
/// then on: a success logs out back to the login screen.
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
    try {
      final profileProvider = context.read<ProfileProvider>();
      await profileProvider.changePassword(
        ChangePasswordRequest(
          currentPassword: _currentPasswordController.text,
          newPassword: _newPasswordController.text,
          // The backend's ChangePasswordRequest.ConfirmNewPassword is a
          // required member — omitting it fails JSON model binding before
          // validation even runs.
          confirmNewPassword: _confirmPasswordController.text,
        ),
      );
      if (!mounted) return false;
      // The old token is dead from this point on: pop back to the login
      // route and clear the local session directly instead of POSTing
      // `auth/logout` with the revoked token. Returning false keeps the
      // scaffold from popping or snackbar-ing a second time. The master
      // screen stops polling on the auth change.
      navigator.popUntil((route) => route.isFirst);
      authState.logout();
      messenger.showSnackBar(
        const SnackBar(
          content: Text('Lozinka je promijenjena. Prijavite se ponovo.'),
        ),
      );
      return false;
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
      return false;
    }
  }

  void _clearFields() {
    _currentPasswordController.clear();
    _newPasswordController.clear();
    _confirmPasswordController.clear();
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: 'Promijeni lozinku',
      presentation: EntityFormPresentation.dialog,
      onSave: _save,
      onReset: _clearFields,
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
          validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _confirmPasswordController,
          decoration: const InputDecoration(labelText: 'Potvrdi novu lozinku'),
          obscureText: true,
          validator: Validators.confirmPassword(_newPasswordController.text),
        ),
      ],
    );
  }
}
