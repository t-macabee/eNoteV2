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
            size: AppSize.avatarRadiusLg * 2,
            borderRadius: AppSize.avatarRadiusLg,
            imagePicker: imagePicker,
            onUpload: onUpload,
          ),
        ),
        const SizedBox(height: AppSpacing.lg),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            OutlinedButton.icon(
              onPressed: onChangePicture,
              icon: const Icon(Icons.upload_outlined),
              label: const Text('Promijeni sliku'),
            ),
            const SizedBox(width: AppSpacing.md),
            OutlinedButton.icon(
              onPressed: onRemovePicture,
              icon: const Icon(Icons.delete_outline),
              label: const Text('Ukloni sliku'),
            ),
          ],
        ),
        if (!hasPicture) ...[
          const SizedBox(height: AppSpacing.xs),
          Text(
            'Nema profilne slike',
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodySmall
                ?.copyWith(color: AppTheme.textTertiary),
          ),
        ],
        if (pictureError != null) ...[
          const SizedBox(height: AppSpacing.sm),
          Text(
            pictureError!,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodySmall
                ?.copyWith(color: AppTheme.error),
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
          AppTextField(
            controller: firstNameController,
            label: 'Ime',
            maxLength: 256,
          ),
          const SizedBox(height: AppSpacing.lg),
          AppTextField(
            controller: lastNameController,
            label: 'Prezime',
            maxLength: 256,
          ),
          const SizedBox(height: AppSpacing.lg),
          AppTextField(
            controller: emailController,
            label: 'Email *',
            keyboardType: TextInputType.emailAddress,
            validator: Validators.email,
            maxLength: 256,
          ),
          const SizedBox(height: AppSpacing.lg),
          AppDatePickerField(
            label: 'Datum rođenja',
            value: dateOfBirth,
            lastDate: DateTime.now(),
            allowClear: false,
            onChanged: onDateOfBirthChanged,
          ),
        ],
      ),
    );
  }
}
