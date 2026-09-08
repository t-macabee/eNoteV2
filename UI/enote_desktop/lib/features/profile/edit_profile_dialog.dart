import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../theme/app_theme.dart';
import '../../widgets/date_field.dart';
import '../../widgets/entity_form_scaffold.dart';
import '../../widgets/image_upload_helper.dart';
import 'profile_provider.dart';

/// "Uredi" form opened from [ProfileDialog] — same pattern as editing a
/// music store: its own [EntityFormScaffold] with its own Save/Cancel.
/// Unlike an entity edit (which closes on save), this one stays open and
/// clears its fields on success, matching the change-password dialog.
class EditProfileDialog extends StatefulWidget {
  final String? initialFirstName;
  final String? initialLastName;
  final String? initialEmail;
  final DateTime? initialDateOfBirth;
  final bool initialHasPicture;

  const EditProfileDialog({
    super.key,
    this.initialFirstName,
    this.initialLastName,
    this.initialEmail,
    this.initialDateOfBirth,
    this.initialHasPicture = false,
  });

  @override
  State<EditProfileDialog> createState() => _EditProfileDialogState();
}

class _EditProfileDialogState extends State<EditProfileDialog> {
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  DateTime? _dateOfBirth;
  bool _hasPicture = false;
  // Bumped after every upload/delete so the `me/picture` URL reloads —
  // the URL is cacheable, so `?v=` (plus ImageField's new imageUrl) is what
  // makes the new bytes appear without reopening the dialog.
  int _pictureVersion = 0;

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
    _hasPicture = widget.initialHasPicture;
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
      final profileProvider = context.read<ProfileProvider>();
      await profileProvider.updateProfile(
        UpdateProfileRequest(
          email: _emailController.text,
          firstName: _firstNameController.text,
          lastName: _lastNameController.text,
          dateOfBirth: _dateOfBirth,
        ),
      );
      return true;
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
      return false;
    }
  }

  Future<String?> _uploadImage(
      Uint8List bytes, String fileName, String contentType) async {
    try {
      final provider = context.read<ProfileProvider>();
      await provider.uploadPicture(bytes, fileName, contentType);
      if (mounted) {
        setState(() {
          _hasPicture = true;
          _pictureVersion++;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Slika uspješno postavljena.')),
        );
      }
      return provider.pictureUrl(cacheBuster: _pictureVersion);
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
      return null;
    }
  }

  Future<void> _removePicture() async {
    try {
      await context.read<ProfileProvider>().deletePicture();
      if (mounted) {
        setState(() {
          _hasPicture = false;
          _pictureVersion++;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Slika uklonjena.')),
        );
      }
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
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
      isEditMode: true,
      onSave: _save,
      onReset: _clearFields,
      fieldsBuilder: (context) => [
        const Text('Slika', style: TextStyle(fontWeight: FontWeight.bold)),
        const Text(
          'Slika se automatski sprema prilikom odabira.',
          style: TextStyle(
            fontSize: 12,
            color: AppTheme.textSecondary,
          ),
        ),
        Align(
          alignment: Alignment.centerLeft,
          child: ImageField(
            key: ValueKey('profile-picture-$_pictureVersion-$_hasPicture'),
            imageUrl: _hasPicture
                ? context
                    .read<ProfileProvider>()
                    .pictureUrl(cacheBuster: _pictureVersion)
                : null,
            imagePicker: pickImageBytes,
            onUpload: _uploadImage,
            apiClient: context.read<ApiClient>(),
          ),
        ),
        if (_hasPicture)
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton.icon(
              onPressed: _removePicture,
              icon: const Icon(Icons.delete_outline),
              label: const Text('Ukloni sliku'),
            ),
          ),
        TextFormField(
          controller: _firstNameController,
          decoration: const InputDecoration(labelText: 'Ime'),
          validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
        ),
        TextFormField(
          controller: _lastNameController,
          decoration: const InputDecoration(labelText: 'Prezime'),
          validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
        ),
        TextFormField(
          controller: _emailController,
          decoration: const InputDecoration(labelText: 'Email'),
          validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
        ),
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
