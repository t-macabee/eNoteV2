import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_form_sections.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import '../../../widgets/image_upload_helper.dart';
import 'shop_store_provider.dart';

class ShopStoreFormScreen extends StatefulWidget {
  final MusicStoreDto store;
  final EntityFormPresentation presentation;

  const ShopStoreFormScreen({
    super.key,
    required this.store,
    this.presentation = EntityFormPresentation.dialog,
  });

  @override
  State<ShopStoreFormScreen> createState() => _ShopStoreFormScreenState();
}

class _ShopStoreFormScreenState extends State<ShopStoreFormScreen>
    with FormControllerLifecycle {
  late final _businessHoursController = textController();
  late final _phoneNumberController = textController();
  String? _currentImagePath;

  @override
  void initState() {
    super.initState();
    final s = widget.store;
    _businessHoursController.text = s.businessHours;
    _phoneNumberController.text = s.phoneNumber ?? '';
    _currentImagePath = s.imagePath;
  }

  Future<String?> _uploadImage(
    Uint8List bytes,
    String fileName,
    String contentType,
  ) {
    final provider = context.read<ShopStoreProvider>();
    return uploadImageWith(
      () => provider.uploadOwnStoreImage(bytes, fileName, contentType).then((
        updated,
      ) {
        if (mounted) {
          setState(() {
            _currentImagePath = updated.imagePath;
          });
        }
        return updated.imagePath;
      }),
      context: context,
    );
  }

  Future<bool> _save() async {
    final provider = context.read<ShopStoreProvider>();
    final phone = _phoneNumberController.text.trim();
    final request = ShopStoreUpdateRequest(
      businessHours: _businessHoursController.text.trim(),
      phoneNumber: phone.isEmpty ? null : phone,
    );

    await provider.updateOwnStore(request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      savedMessage: 'Prodavnica je sačuvana.',
      presentation: widget.presentation,
      title: 'Uredi prodavnicu',
      isEditMode: true,
      fieldsBuilder: (_) => [
        FormSection(
          title: 'Osnovni podaci',
          children: [
            InfoRow(label: 'Naziv', value: widget.store.storeName),
            InfoRow(label: 'Adresa', value: widget.store.addressText),
            Text(
              'Naziv i adresu mijenja administrator.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
            FormFieldRow(
              children: [
                EntityTextField(
                  controller: _businessHoursController,
                  label: 'Radno vrijeme',
                  validator: Validators.businessHours,
                  maxLength: 50,
                ),
                EntityTextField(
                  controller: _phoneNumberController,
                  label: 'Broj telefona',
                  required: false,
                  validator: Validators.optionalPhone,
                  maxLength: 30,
                ),
              ],
            ),
          ],
        ),
        FormSection(
          title: 'Slika',
          description: 'Slika se automatski sprema prilikom odabira.',
          children: [
            Align(
              alignment: Alignment.centerLeft,
              child: ImageField(
                imageUrl: _currentImagePath,
                imagePicker: () => pickImage(context),
                onUpload: _uploadImage,
                editable: true,
                apiClient: context.read<ApiClient>(),
              ),
            ),
          ],
        ),
      ],
      onSave: _save,
    );
  }
}
