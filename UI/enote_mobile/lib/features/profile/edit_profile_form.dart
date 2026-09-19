import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';
import '../../widgets/app_date_picker_field.dart';
import '../../widgets/app_text_field.dart';

class EditProfilePictureSection extends StatelessWidget {
  final String? pictureUrl;
  final ApiClient apiClient;
  final bool hasPicture;
  final String? pictureError;
  final Future<Uint8List?> Function() imagePicker;
  final ImageUploadCallback onUpload;
  final VoidCallback? onChangePicture;
  final VoidCallback? onRemovePicture;

  const EditProfilePictureSection({
    super.key,
    required this.pictureUrl,
    required this.apiClient,
    required this.hasPicture,
    required this.pictureError,
    required this.imagePicker,
    required this.onUpload,
    required this.onChangePicture,
    required this.onRemovePicture,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Center(
          child: ImageField(
            imageUrl: pictureUrl,
            apiClient: apiClient,
            size: 96,
            borderRadius: 48,
            imagePicker: imagePicker,
            onUpload: onUpload,
          ),
        ),
        const SizedBox(height: 16),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            OutlinedButton.icon(
              onPressed: onChangePicture,
              icon: const Icon(Icons.upload_outlined),
              label: const Text('Promijeni sliku'),
            ),
            const SizedBox(width: 12),
            OutlinedButton.icon(
              onPressed: onRemovePicture,
              icon: const Icon(Icons.delete_outline),
              label: const Text('Ukloni sliku'),
            ),
          ],
        ),
        if (!hasPicture) ...[
          const SizedBox(height: 4),
          const Text(
            'Nema profilne slike',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppTheme.textTertiary, fontSize: 12),
          ),
        ],
        if (pictureError != null) ...[
          const SizedBox(height: 8),
          Text(
            pictureError!,
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppTheme.error, fontSize: 13),
          ),
        ],
      ],
    );
  }
}

class EditProfileFields extends StatelessWidget {
  final GlobalKey<FormState> formKey;
  final TextEditingController firstNameController;
  final TextEditingController lastNameController;
  final TextEditingController emailController;
  final DateTime? dateOfBirth;
  final ValueChanged<DateTime?> onDateOfBirthChanged;

  const EditProfileFields({
    super.key,
    required this.formKey,
    required this.firstNameController,
    required this.lastNameController,
    required this.emailController,
    required this.dateOfBirth,
    required this.onDateOfBirthChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Form(
      key: formKey,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      child: Column(
        children: [
          AppTextField(controller: firstNameController, label: 'Ime'),
          const SizedBox(height: 16),
          AppTextField(controller: lastNameController, label: 'Prezime'),
          const SizedBox(height: 16),
          AppTextField(
            controller: emailController,
            label: 'Email *',
            keyboardType: TextInputType.emailAddress,
            validator: Validators.email,
          ),
          const SizedBox(height: 16),
          AppDatePickerField(
            label: 'Datum rođenja',
            value: dateOfBirth,
            lastDate: DateTime.now(),
            onChanged: onDateOfBirthChanged,
          ),
        ],
      ),
    );
  }
}
