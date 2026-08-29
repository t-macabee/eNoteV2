import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
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

class _InstrumentFormScreenState extends State<InstrumentFormScreen> {
  final _modelController = TextEditingController();
  final _manufacturerController = TextEditingController();
  final _descriptionController = TextEditingController();

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
      _currentImagePath = existing.imagePath;
      _selectedInstrumentTypeId = existing.instrumentTypeId;
    }
  }

  @override
  void dispose() {
    _modelController.dispose();
    _manufacturerController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<Uint8List?> _pickImageBytes() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.image,
      withData: true,
    );
    if (result == null || result.files.isEmpty) return null;
    return result.files.first.bytes;
  }

  Future<String?> _uploadImage(
      Uint8List bytes, String fileName, String contentType) async {
    final provider = context.read<InstrumentProvider>();
    try {
      final updated = await provider.uploadImage(
        widget.existing!.id,
        bytes,
        fileName,
        contentType,
      );
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
    final provider = context.read<InstrumentProvider>();

    if (!_isEditMode) {
      final request = InstrumentCreateRequest(
        model: _modelController.text.trim(),
        manufacturer: _manufacturerController.text.trim(),
        description: _descriptionController.text.trim().isEmpty
            ? null
            : _descriptionController.text.trim(),
        instrumentTypeId: _selectedInstrumentTypeId!,
      );
      await provider.insert(request.toJson());
    } else {
      final request = InstrumentUpdateRequest(
        model: _modelController.text.trim(),
        manufacturer: _manufacturerController.text.trim(),
        description: _descriptionController.text.trim().isEmpty
            ? null
            : _descriptionController.text.trim(),
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
        TextFormField(
          controller: _modelController,
          decoration: const InputDecoration(labelText: 'Model'),
          validator: Validators.required('Model'),
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: _manufacturerController,
          decoration: const InputDecoration(labelText: 'Proizvođač'),
          validator: Validators.required('Proizvođač'),
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: _descriptionController,
          decoration: const InputDecoration(labelText: 'Opis'),
          maxLines: 3,
          minLines: 1,
        ),
        const SizedBox(height: 12),
        AsyncDropdown<InstrumentTypeDto>(
          label: 'Tip instrumenta',
          value: _selectedInstrumentTypeId,
          fetcher: () async {
            final provider = context.read<ShopInstrumentTypeProvider>();
            final result = await provider.search({
              'page': 1,
              'pageSize': 100,
              'includeTotalCount': true,
            });
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
          const SizedBox(height: 24),
          const Text('Slika', style: TextStyle(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          ImageField(
            imageUrl: _currentImagePath,
            imagePicker: _pickImageBytes,
            onUpload: _uploadImage,
            apiClient: context.read<ApiClient>(),
          ),
        ],
      ],
      onSave: _save,
      onReset: () {
        _modelController.clear();
        _manufacturerController.clear();
        _descriptionController.clear();
        setState(() {
          _selectedInstrumentTypeId = null;
          _currentImagePath = null;
        });
      },
    );
  }
}
