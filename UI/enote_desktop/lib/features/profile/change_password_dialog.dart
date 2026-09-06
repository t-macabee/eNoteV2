import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../widgets/entity_form_scaffold.dart';

/// "Promijeni lozinku" form opened from [ProfileDialog] — its own
/// [EntityFormScaffold] with its own Save/Cancel. Stays open and clears its
/// fields on success rather than closing.
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
    try {
      final apiClient = context.read<ApiClient>();
      final response = await apiClient.put(
        'users/me/password',
        body: {
          'currentPassword': _currentPasswordController.text,
          'newPassword': _newPasswordController.text,
          // The backend's ChangePasswordRequest.ConfirmNewPassword is a
          // required member — omitting it fails JSON model binding before
          // validation even runs.
          'confirmNewPassword': _confirmPasswordController.text,
        },
      );
      if (response.statusCode >= 400) {
        throw ApiException(
          ApiErrorMapper.mapError(response.statusCode, response.body),
        );
      }
      return true;
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
      savedMessage: 'Lozinka uspješno promijenjena.',
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
