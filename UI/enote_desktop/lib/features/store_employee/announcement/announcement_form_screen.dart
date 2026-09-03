import 'package:enote_core/enote_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/image_upload_helper.dart';
import 'announcement_provider.dart';

class AnnouncementFormScreen extends StatefulWidget {
  final AnnouncementDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const AnnouncementFormScreen({
    super.key,
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

  Future<String?> _uploadImage(
      Uint8List bytes, String fileName, String contentType) {
    final provider = context.read<StoreAnnouncementProvider>();
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
    final provider = context.read<StoreAnnouncementProvider>();
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
        const SizedBox(height: 18),
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
              apiClient: context.read<ApiClient>(),
            ),
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
