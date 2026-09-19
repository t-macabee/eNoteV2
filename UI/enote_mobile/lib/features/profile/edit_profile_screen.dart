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

const _allowedPictureExtensions = ['jpg', 'jpeg', 'png', 'webp'];
const _pictureErrorCopy = 'Dozvoljeni formati: JPG, PNG, WebP (do 5 MB).';
const _pictureSizeLimit = 5 * 1024 * 1024;

class _PickedImage {
  final Uint8List bytes;
  final String fileName;
  final String contentType;

  _PickedImage({
    required this.bytes,
    required this.fileName,
    required this.contentType,
  });
}

/// S31 — profile edit form (incl. picture upload/remove). Reached only from
/// S30, so [SessionController.profile] is loaded when this route opens.
/// No password field anywhere (the old-password gate is the separate S32).
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
          firstName: firstName.isEmpty ? null : firstName,
          lastName: lastName.isEmpty ? null : lastName,
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

  Future<_PickedImage?> _pickImage() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.custom,
      allowedExtensions: _allowedPictureExtensions,
      withData: true,
    );
    if (result == null || result.files.isEmpty) return null;
    final file = result.files.single;
    final bytes = file.bytes;
    if (bytes == null) return null;
    final fileName = file.name;
    final dot = fileName.lastIndexOf('.');
    final extension =
        dot == -1 ? '' : fileName.substring(dot + 1).toLowerCase();
    final extensionOk = _allowedPictureExtensions.contains(extension);
    if (!extensionOk || bytes.length > _pictureSizeLimit) {
      if (mounted) {
        setState(() => _pictureError = _pictureErrorCopy);
      }
      return null;
    }
    if (mounted) {
      setState(() => _pictureError = null);
    }
    return _PickedImage(
      bytes: bytes,
      fileName: fileName,
      contentType: switch (extension) {
        'jpg' || 'jpeg' => 'image/jpeg',
        'png' => 'image/png',
        'webp' => 'image/webp',
        _ => 'application/octet-stream',
      },
    );
  }

  /// Shared by the *Promijeni sliku* button and the avatar-tap upload.
  Future<void> _uploadPicked(_PickedImage picked) async {
    final provider = context.read<ProfileProvider>();
    final session = context.read<SessionController>();
    await submitWith(
      (busy) => _pictureBusy = busy,
      () async {
        await provider.uploadPicture(
          picked.bytes,
          picked.fileName,
          picked.contentType,
        );
        await session.reloadProfile();
      },
    );
  }

  Future<void> _changePicture() async {
    final picked = await _pickImage();
    if (picked == null || !mounted) return;
    await _uploadPicked(picked);
  }

  /// Adapter for core [ImageField]: validation only, upload happens through
  /// the widget's own [ImageField.onUpload] pipeline.
  Future<Uint8List?> _imagePickerBytes() async =>
      (await _pickImage())?.bytes;

  Future<String?> _imageFieldUpload(
    Uint8List bytes,
    String fileName,
    String contentType,
  ) async {
    final session = context.read<SessionController>();
    final apiClient = context.read<ApiClient>();
    await _uploadPicked(
      _PickedImage(bytes: bytes, fileName: fileName, contentType: contentType),
    );
    if (!mounted) return null;
    return userPictureUrl(
      apiClient,
      'me',
      cacheBuster: session.pictureVersion,
    );
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
    await submitWith(
      (busy) => _pictureBusy = busy,
      () async {
        await provider.deletePicture();
        await session.reloadProfile();
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final session = context.watch<SessionController>();
    final profile = session.profile;
    if (profile == null) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    final hasPicture = profile.hasPicture;
    final pictureUrl = hasPicture
        ? userPictureUrl(
            context.read<ApiClient>(),
            'me',
            cacheBuster: session.pictureVersion,
          )
        : null;

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
          imagePicker: _imagePickerBytes,
          onUpload: _imageFieldUpload,
          onChangePicture: _pictureBusy ? null : _changePicture,
          onRemovePicture:
              (!hasPicture || _pictureBusy) ? null : _removePicture,
        ),
        const SizedBox(height: 20),
        EditProfileFields(
          formKey: _formKey,
          firstNameController: _firstNameController,
          lastNameController: _lastNameController,
          emailController: _emailController,
          dateOfBirth: _dateOfBirth,
          onDateOfBirthChanged: (value) =>
              setState(() => _dateOfBirth = value),
        ),
      ],
    );
  }
}
