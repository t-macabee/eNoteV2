import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../widgets/date_field.dart';
import '../../widgets/entity_form_scaffold.dart';

/// "Uredi" form opened from [ProfileDialog] — same pattern as editing a
/// music store: its own [EntityFormScaffold] with its own Save/Cancel.
/// Unlike an entity edit (which closes on save), this one stays open and
/// clears its fields on success, matching the change-password dialog.
class EditProfileDialog extends StatefulWidget {
  final String? initialFirstName;
  final String? initialLastName;
  final String? initialEmail;
  final DateTime? initialDateOfBirth;

  const EditProfileDialog({
    super.key,
    this.initialFirstName,
    this.initialLastName,
    this.initialEmail,
    this.initialDateOfBirth,
  });

  @override
  State<EditProfileDialog> createState() => _EditProfileDialogState();
}

class _EditProfileDialogState extends State<EditProfileDialog> {
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  DateTime? _dateOfBirth;

  // Bumped on every clear so the DateField below gets a fresh key — a
  // FormField ignores a changed initialValue on rebuild once mounted, so
  // recreating it is the only way to make it pick up the cleared value.
  int _resetGeneration = 0;

  @override
  void initState() {
    super.initState();
    _firstNameController.text = widget.initialFirstName ?? '';
    _lastNameController.text = widget.initialLastName ?? '';
    _emailController.text = widget.initialEmail ?? '';
    _dateOfBirth = widget.initialDateOfBirth;
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    try {
      final apiClient = context.read<ApiClient>();
      final response = await apiClient.put(
        'users/me',
        body: {
          'email': _emailController.text,
          'firstName': _firstNameController.text,
          'lastName': _lastNameController.text,
          'dateOfBirth': _dateOfBirth?.toIso8601String().split('T').first,
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
    _firstNameController.clear();
    _lastNameController.clear();
    _emailController.clear();
    _dateOfBirth = null;
    _resetGeneration++;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: 'Uredi profil',
      presentation: EntityFormPresentation.dialog,
      onSave: _save,
      onReset: _clearFields,
      fieldsBuilder: (context) => [
        TextFormField(
          controller: _firstNameController,
          decoration: const InputDecoration(labelText: 'Ime'),
          validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _lastNameController,
          decoration: const InputDecoration(labelText: 'Prezime'),
          validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _emailController,
          decoration: const InputDecoration(labelText: 'Email'),
          validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
        ),
        const SizedBox(height: 16),
        DateField(
          key: ValueKey(_resetGeneration),
          labelText: 'Datum rođenja',
          initialValue: _dateOfBirth,
          firstDate: DateTime(1900),
          onChanged: (v) => _dateOfBirth = v,
        ),
      ],
    );
  }
}
