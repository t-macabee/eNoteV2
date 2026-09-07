import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
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

class _ShopStoreFormScreenState extends State<ShopStoreFormScreen> {
  final _storeNameController = TextEditingController();
  final _businessHoursController = TextEditingController();
  final _phoneNumberController = TextEditingController();
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

  @override
  void dispose() {
    _storeNameController.dispose();
    _businessHoursController.dispose();
    _phoneNumberController.dispose();
    super.dispose();
  }

  Future<String?> _uploadImage(
    Uint8List bytes,
    String fileName,
    String contentType,
  ) async {
    final provider = context.read<ShopStoreProvider>();
    try {
      final updated =
          await provider.uploadOwnStoreImage(bytes, fileName, contentType);
      if (mounted) {
        setState(() {
          _currentImagePath = updated.imagePath;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Slika uspješno postavljena.')),
        );
      }
      return updated.imagePath;
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
      return null;
    }
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
        TextFormField(
          controller: _storeNameController,
          decoration: const InputDecoration(labelText: 'Naziv'),
          validator: Validators.required('Naziv'),
        ),
        const SizedBox(height: 18),
        TextFormField(
          controller: _businessHoursController,
          decoration: const InputDecoration(labelText: 'Radno vrijeme'),
          validator: Validators.required('Radno vrijeme'),
        ),
        const SizedBox(height: 18),
        TextFormField(
          controller: _phoneNumberController,
          decoration: const InputDecoration(labelText: 'Broj telefona'),
        ),
        const SizedBox(height: 18),
        AsyncDropdown<AddressReferenceDto>(
          label: 'Adresa',
          value: _selectedAddressId,
          fetcher: () async {
            final provider = context.read<ShopAddressProvider>();
            final result = await provider.search({
              'page': 1,
              'pageSize': 100,
            });
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
        const SizedBox(height: 24),
        const Text('Slika', style: TextStyle(fontWeight: FontWeight.bold)),
        const SizedBox(height: 4),
        const Text(
          'Slika se automatski sprema prilikom odabira.',
          style: TextStyle(
            fontSize: 12,
            color: AppTheme.textSecondary,
          ),
        ),
        const SizedBox(height: 8),
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
