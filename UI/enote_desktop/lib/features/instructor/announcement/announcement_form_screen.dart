import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'announcement_provider.dart';

class AnnouncementFormScreen extends StatefulWidget {
  final int courseId;
  final AnnouncementDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const AnnouncementFormScreen({
    super.key,
    required this.courseId,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<AnnouncementFormScreen> createState() => _AnnouncementFormScreenState();
}

class _AnnouncementFormScreenState extends State<AnnouncementFormScreen> {
  final _titleController = TextEditingController();
  final _contentController = TextEditingController();
  String? _currentImagePath;

  bool get _isEditMode => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _titleController.text = existing.title;
      _contentController.text = existing.content;
      _currentImagePath = existing.imagePath;
    }
  }

  @override
  void dispose() {
    _titleController.dispose();
    _contentController.dispose();
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
    final provider = context.read<AnnouncementProvider>();
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
    final provider = context.read<AnnouncementProvider>();
    final request = AnnouncementRequest(
      title: _titleController.text.trim(),
      content: _contentController.text.trim(),
    );

    if (!_isEditMode) {
      await provider.insert(request.toJson());
    } else {
      await provider.update(widget.existing!.id, request.toJson());
    }
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: _isEditMode ? 'Uredi objavu' : 'Dodaj objavu',
      isEditMode: _isEditMode,
      fieldsBuilder: (_) => [
        TextFormField(
          controller: _titleController,
          decoration: const InputDecoration(labelText: 'Naslov'),
          validator: Validators.required('Naslov'),
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: _contentController,
          decoration: const InputDecoration(labelText: 'Sadržaj'),
          maxLines: 8,
          minLines: 4,
          validator: Validators.required('Sadržaj'),
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
        _titleController.clear();
        _contentController.clear();
        setState(() => _currentImagePath = null);
      },
    );
  }
}
