import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import '../../../widgets/image_upload_helper.dart';
import 'shop_address_provider.dart';
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
  late final _storeNameController = textController();
  late final _businessHoursController = textController();
  late final _phoneNumberController = textController();
  int? _selectedAddressId;
  String? _currentImagePath;

  @override
  void initState() {
    super.initState();
    final s = widget.store;
    _storeNameController.text = s.storeName;
    _businessHoursController.text = s.businessHours;
    _phoneNumberController.text = s.phoneNumber ?? '';
    _selectedAddressId = s.addressId;
    _currentImagePath = s.imagePath;
  }

  Future<String?> _uploadImage(
    Uint8List bytes,
    String fileName,
    String contentType,
  ) {
    final provider = context.read<ShopStoreProvider>();
    return uploadImageWith(
      () => provider
          .uploadOwnStoreImage(bytes, fileName, contentType)
          .then((updated) {
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
    final request = MusicStoreRequest(
      storeName: _storeNameController.text.trim(),
      businessHours: _businessHoursController.text.trim(),
      phoneNumber: phone.isEmpty ? null : phone,
      addressId: _selectedAddressId,
    );

    await provider.updateOwnStore(request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: 'Uredi prodavnicu',
      isEditMode: true,
      fieldsBuilder: (_) => [
        EntityTextField(controller: _storeNameController, label: 'Naziv'),
        EntityTextField(
          controller: _businessHoursController,
          label: 'Radno vrijeme',
        ),
        EntityTextField(
          controller: _phoneNumberController,
          label: 'Broj telefona',
          required: false,
          validator: Validators.optionalPhone,
        ),
        AsyncDropdown<AddressReferenceDto>(
          label: 'Adresa',
          value: _selectedAddressId,
          fetcher: () async {
            final provider = context.read<ShopAddressProvider>();
            final result = await provider.search(pagedQuery(1, 100, ''));
            return result.items;
          },
          itemLabel: (item) => '${item.street} ${item.number}, ${item.city}',
          itemId: (item) => item.id,
          onChanged: (id, item) {
            setState(() {
              _selectedAddressId = id as int?;
            });
          },
        ),
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
            imageUrl: _currentImagePath,
            imagePicker: pickImageBytes,
            onUpload: _uploadImage,
            editable: true,
            apiClient: context.read<ApiClient>(),
          ),
        ),
      ],
      onSave: _save,
    );
  }
}
