import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../widgets/form_submit_state.dart';
import '../../widgets/mobile_form_scaffold.dart';
import 'edit_profile_form.dart';
import 'profile_provider.dart';

class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({super.key});

  @override
  State<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen>
    with FormSubmitState<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _firstNameController;
  late final TextEditingController _lastNameController;
  late final TextEditingController _emailController;
  DateTime? _dateOfBirth;
  bool _pictureBusy = false;
  String? _pictureError;

  @override
  void initState() {
    super.initState();
    final profile = context.read<SessionController>().profile;
    _firstNameController = TextEditingController(
      text: profile?.profile.firstName ?? '',
    );
    _lastNameController = TextEditingController(
      text: profile?.profile.lastName ?? '',
    );
    _emailController = TextEditingController(text: profile?.email ?? '');
    _dateOfBirth = profile?.profile.dateOfBirth;
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    final messenger = ScaffoldMessenger.of(context);
    final provider = context.read<ProfileProvider>();
    final session = context.read<SessionController>();
    await submit(() async {
      final firstName = _firstNameController.text.trim();
      final lastName = _lastNameController.text.trim();
      await provider.updateMe(
        UpdateProfileRequest(
          email: _emailController.text.trim(),
          firstName: firstName,
          lastName: lastName,
          dateOfBirth: _dateOfBirth,
        ),
      );
      await session.reloadProfile();
      if (!mounted) return;
      messenger.showSnackBar(
        const SnackBar(content: Text('Profil je uspješno ažuriran.')),
      );
      Navigator.of(context).pop();
    });
  }

  Future<PickedImage?> _pickImage() async {
    final file = await FilePicker.pickFile(
      type: FileType.custom,
      allowedExtensions: allowedImageExtensions,
    );
    if (file == null) return null;
    final Uint8List bytes;
    try {
      bytes = await file.readAsBytes();
    } on Exception {
      return null;
    }
    final picked = pickedImageOrNull(bytes, file.name);
    if (mounted) {
      setState(() => _pictureError = picked == null ? imageRulesMessage : null);
    }
    return picked;
  }

  Future<bool> _uploadPicked(
    Uint8List bytes,
    String fileName,
    String contentType,
  ) async {
    final provider = context.read<ProfileProvider>();
    final session = context.read<SessionController>();
    var uploaded = false;
    await submitWith((busy) => _pictureBusy = busy, () async {
      await provider.uploadPicture(bytes, fileName, contentType);
      await session.reloadProfile();
      uploaded = true;
    }, showBanner: true);
    return uploaded;
  }

  Future<void> _changePicture() async {
    final picked = await _pickImage();
    if (picked == null || !mounted) return;
    await _uploadPicked(picked.bytes, picked.fileName, picked.contentType);
  }

  Future<String?> _imageFieldUpload(
    Uint8List bytes,
    String fileName,
    String contentType,
  ) async {
    final session = context.read<SessionController>();
    final apiClient = context.read<ApiClient>();
    final uploaded = await _uploadPicked(bytes, fileName, contentType);
    if (!uploaded || !mounted) return null;
    return userPictureUrl(apiClient, 'me', cacheBuster: session.pictureVersion);
  }

  Future<void> _removePicture() async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Uklanjanje slike',
      message: 'Ukloniti profilnu sliku?',
    );
    if (confirmed != true || !mounted) return;
    final provider = context.read<ProfileProvider>();
    final session = context.read<SessionController>();
    await submitWith((busy) => _pictureBusy = busy, () async {
      await provider.deletePicture();
      await session.reloadProfile();
    }, showBanner: true);
  }

  @override
  Widget build(BuildContext context) {
    final session = context.watch<SessionController>();
    final profile = session.profile;
    if (profile == null) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    final hasPicture = profile.hasPicture;
    final pictureUrl = userPictureUrl(
      context.read<ApiClient>(),
      'me',
      cacheBuster: session.pictureVersion,
    );

    return MobileFormScaffold(
      title: 'Uredi profil',
      submitLabel: 'Sačuvaj promjene',
      onSubmit: _save,
      isBusy: isSubmitting || _pictureBusy,
      errorMessage: submitError,
      children: [
        EditProfilePictureSection(
          pictureUrl: pictureUrl,
          apiClient: context.read<ApiClient>(),
          hasPicture: hasPicture,
          pictureError: _pictureError,
          imagePicker: _pickImage,
          onUpload: _imageFieldUpload,
          onChangePicture: _pictureBusy ? null : _changePicture,
          onRemovePicture: (!hasPicture || _pictureBusy)
              ? null
              : _removePicture,
        ),
        const SizedBox(height: AppSpacing.xl),
        EditProfileFields(
          formKey: _formKey,
          firstNameController: _firstNameController,
          lastNameController: _lastNameController,
          emailController: _emailController,
          dateOfBirth: _dateOfBirth,
          onDateOfBirthChanged: (value) => setState(() => _dateOfBirth = value),
        ),
      ],
    );
  }
}
