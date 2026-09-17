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
/// music store: its own [EntityFormScaffold] with its own Save/Cancel, which
/// pops on save since the scaffold runs in edit mode.
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
      final firstName = _firstNameController.text.trim();
      final lastName = _lastNameController.text.trim();
      await profileProvider.updateProfile(
        UpdateProfileRequest(
          email: _emailController.text.trim(),
          firstName: firstName.isEmpty ? null : firstName,
          lastName: lastName.isEmpty ? null : lastName,
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
      Uint8List bytes, String fileName, String contentType) {
    final provider = context.read<ProfileProvider>();
    return uploadImageWith(
      () async {
        await provider.uploadPicture(bytes, fileName, contentType);
        if (mounted) {
          setState(() {
            _hasPicture = true;
            _pictureVersion++;
          });
        }
        return provider.pictureUrl(cacheBuster: _pictureVersion);
      },
      context: context,
    );
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

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: 'Uredi profil',
      presentation: EntityFormPresentation.dialog,
      isEditMode: true,
      onSave: _save,
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
        ),
        TextFormField(
          controller: _lastNameController,
          decoration: const InputDecoration(labelText: 'Prezime'),
        ),
        TextFormField(
          controller: _emailController,
          decoration: const InputDecoration(labelText: 'Email'),
          validator: Validators.email,
        ),
        DateField(
          labelText: 'Datum rođenja',
          initialValue: _dateOfBirth,
          firstDate: DateTime(1900),
          lastDate: DateTime.now(),
          validator: (v) => v != null && v.isAfter(DateTime.now())
              ? 'Datum rođenja ne može biti u budućnosti.'
              : null,
          onChanged: (v) => _dateOfBirth = v,
        ),
      ],
    );
  }
}
