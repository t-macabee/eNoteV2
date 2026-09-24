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
import '../address/address_provider.dart';
import 'music_store_provider.dart';

class MusicStoreFormScreen extends StatefulWidget {
  final MusicStoreDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const MusicStoreFormScreen({
    super.key,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<MusicStoreFormScreen> createState() => _MusicStoreFormScreenState();
}

class _MusicStoreFormScreenState extends State<MusicStoreFormScreen>
    with FormControllerLifecycle {
  late final _storeNameController = textController();
  late final _businessHoursController = textController();
  late final _phoneNumberController = textController();
  int? _selectedAddressId;
  String? _currentImagePath;

  bool get _isEditMode => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _storeNameController.text = existing.storeName;
      _businessHoursController.text = existing.businessHours;
      _phoneNumberController.text = existing.phoneNumber ?? '';
      _selectedAddressId = existing.addressId;
      _currentImagePath = existing.imagePath;
    }
  }

  Future<String?> _uploadImage(
      Uint8List bytes, String fileName, String contentType) {
    final provider = context.read<MusicStoreProvider>();
    return uploadImageFor(
      provider,
      widget.existing!.id,
      bytes,
      fileName,
      contentType,
      context: context,
      onSuccess: (updated) {
        if (mounted) {
          setState(() {
            _currentImagePath = updated.imagePath;
          });
        }
        return updated.imagePath;
      },
    );
  }

  Future<bool> _save() async {
    final provider = context.read<MusicStoreProvider>();
    final phone = _phoneNumberController.text.trim();
    final request = MusicStoreRequest(
      storeName: _storeNameController.text.trim(),
      businessHours: _businessHoursController.text.trim(),
      phoneNumber: phone.isEmpty ? null : phone,
      addressId: _selectedAddressId,
    );

    await provider.save(id: widget.existing?.id, request: request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: _isEditMode ? 'Uredi prodavnicu' : 'Dodaj prodavnicu',
      isEditMode: _isEditMode,
      onReset: () {
        clearTextControllers();
        setState(() {
          _selectedAddressId = null;
          _currentImagePath = null;
        });
      },
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
            final provider = context.read<AddressProvider>();
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
        if (_isEditMode) ...[
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
              apiClient: context.read<ApiClient>(),
            ),
          ),
        ],
      ],
      onSave: _save,
    );
  }
}
