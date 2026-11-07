import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';
import '../../widgets/date_field.dart';
import '../../widgets/entity_form_scaffold.dart';
import '../../widgets/image_upload_helper.dart';
import 'profile_provider.dart';

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
    final profileProvider = context.read<ProfileProvider>();
    final firstName = _firstNameController.text.trim();
    final lastName = _lastNameController.text.trim();
    await profileProvider.updateProfile(
      UpdateProfileRequest(
        email: _emailController.text.trim(),
        firstName: firstName,
        lastName: lastName,
        dateOfBirth: _dateOfBirth,
      ),
    );
    return true;
  }

  Future<String?> _uploadImage(
    Uint8List bytes,
    String fileName,
    String contentType,
  ) {
    final provider = context.read<ProfileProvider>();
    return uploadImageWith(() async {
      await provider.uploadPicture(bytes, fileName, contentType);
      if (mounted) {
        setState(() => _hasPicture = true);
      }
      return provider.pictureUrl();
    }, context: context);
  }

  Future<void> _removePicture() async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Uklanjanje slike',
      message: 'Ukloniti profilnu sliku?',
    );
    if (confirmed != true || !mounted) return;
    try {
      await context.read<ProfileProvider>().deletePicture();
      if (mounted) {
        setState(() => _hasPicture = false);
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Slika uklonjena.')));
      }
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.read<ProfileProvider>();
    return EntityFormScaffold(
      title: 'Uredi profil',
      presentation: EntityFormPresentation.dialog,
      isEditMode: true,
      onSave: _save,
      fieldsBuilder: (context) => [
        const Text('Slika', style: TextStyle(fontWeight: FontWeight.bold)),
        Text(
          'Slika se automatski sprema prilikom odabira.',
          style: Theme.of(context).textTheme.labelMedium
              ?.copyWith(color: AppTheme.textSecondary),
        ),
        Align(
          alignment: Alignment.centerLeft,
          child: ImageField(
            key: ValueKey(
              'profile-picture-${provider.pictureVersion}-$_hasPicture',
            ),
            imageUrl: provider.pictureUrl(),
            imagePicker: () => pickImage(context),
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
          decoration: const InputDecoration(labelText: 'Ime *'),
          validator: Validators.firstName,
          maxLength: 256,
        ),
        TextFormField(
          controller: _lastNameController,
          decoration: const InputDecoration(labelText: 'Prezime *'),
          validator: Validators.lastName,
          maxLength: 256,
        ),
        TextFormField(
          controller: _emailController,
          decoration: const InputDecoration(
            labelText: 'Email',
            errorMaxLines: 3,
          ),
          validator: Validators.email,
          maxLength: 256,
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
