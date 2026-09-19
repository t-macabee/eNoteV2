import 'package:enote_core/enote_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import '../../../widgets/image_upload_helper.dart';
import 'instrument_provider.dart';
import 'shop_instrument_type_provider.dart';

class InstrumentFormScreen extends StatefulWidget {
  final InstrumentDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const InstrumentFormScreen({
    super.key,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<InstrumentFormScreen> createState() => _InstrumentFormScreenState();
}

class _InstrumentFormScreenState extends State<InstrumentFormScreen>
    with FormControllerLifecycle {
  late final _modelController = textController();
  late final _manufacturerController = textController();
  late final _descriptionController = textController();

  int? _selectedInstrumentTypeId;
  String? _currentImagePath;

  bool get _isEditMode => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _modelController.text = existing.model;
      _manufacturerController.text = existing.manufacturer;
      _descriptionController.text = existing.description ?? '';
      _selectedInstrumentTypeId = existing.instrumentTypeId;
      _currentImagePath = existing.imagePath;
    }
  }

  Future<String?> _uploadImage(
      Uint8List bytes, String fileName, String contentType) {
    final provider = context.read<InstrumentProvider>();
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
    final provider = context.read<InstrumentProvider>();
    final desc = _descriptionController.text.trim();

    if (widget.existing == null) {
      final request = InstrumentCreateRequest(
        model: _modelController.text.trim(),
        manufacturer: _manufacturerController.text.trim(),
        description: desc.isEmpty ? null : desc,
        imagePath: _currentImagePath,
        instrumentTypeId: _selectedInstrumentTypeId!,
      );
      await provider.insert(request.toJson());
    } else {
      final request = InstrumentUpdateRequest(
        model: _modelController.text.trim(),
        manufacturer: _manufacturerController.text.trim(),
        description: desc.isEmpty ? null : desc,
        imagePath: _currentImagePath,
        instrumentTypeId: _selectedInstrumentTypeId,
      );
      await provider.update(widget.existing!.id, request.toJson());
    }
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: _isEditMode ? 'Uredi instrument' : 'Dodaj instrument',
      isEditMode: _isEditMode,
      fieldsBuilder: (_) => [
        EntityTextField(controller: _modelController, label: 'Model'),
        EntityTextField(
          controller: _manufacturerController,
          label: 'Proizvođač',
        ),
        EntityTextField(
          controller: _descriptionController,
          label: 'Opis',
          required: false,
          maxLines: 3,
          minLines: 1,
        ),
        AsyncDropdown<InstrumentTypeDto>(
          label: 'Tip instrumenta',
          value: _selectedInstrumentTypeId,
          fetcher: () async {
            final provider = context.read<ShopInstrumentTypeProvider>();
            final result = await provider.search(pagedQuery(1, 100, ''));
            return result.items;
          },
          itemLabel: (item) => item.type,
          itemId: (item) => item.id,
          onChanged: (id, item) {
            setState(() {
              _selectedInstrumentTypeId = id as int?;
            });
          },
          validator: (value) {
            if (value == null) return 'Tip instrumenta je obavezan';
            return null;
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
      onReset: () {
        clearTextControllers();
        setState(() {
          _selectedInstrumentTypeId = null;
          _currentImagePath = null;
        });
      },
    );
  }
}
