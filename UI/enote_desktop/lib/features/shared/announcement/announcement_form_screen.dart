import 'package:enote_core/enote_core.dart';
import 'package:flutter/material.dart';

import '../../../widgets/entity_form_scaffold.dart';

class AnnouncementFormScreen extends StatefulWidget {
  final BaseProvider<AnnouncementDto> provider;
  final AnnouncementDto? existing;
  final EntityFormPresentation presentation;

  const AnnouncementFormScreen({
    super.key,
    required this.provider,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<AnnouncementFormScreen> createState() => _AnnouncementFormScreenState();
}

class _AnnouncementFormScreenState extends State<AnnouncementFormScreen> {
  final _titleController = TextEditingController();
  final _contentController = TextEditingController();

  bool get _isEditMode => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _titleController.text = existing.title;
      _contentController.text = existing.content;
    }
  }

  @override
  void dispose() {
    _titleController.dispose();
    _contentController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final request = AnnouncementRequest(
      title: _titleController.text.trim(),
      content: _contentController.text.trim(),
    );

    if (!_isEditMode) {
      await widget.provider.insert(request.toJson());
    } else {
      await widget.provider.update(widget.existing!.id, request.toJson());
    }
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: _isEditMode ? 'Uredi objavu' : 'Dodaj objavu',
      isEditMode: _isEditMode,
      closeAfterAdd: true,
      fieldsBuilder: (_) => [
        TextFormField(
          controller: _titleController,
          decoration: const InputDecoration(labelText: 'Naslov'),
          validator: Validators.required('Naslov'),
        ),
        TextFormField(
          controller: _contentController,
          decoration: const InputDecoration(labelText: 'Sadržaj'),
          maxLines: 8,
          minLines: 4,
          validator: Validators.required('Sadržaj'),
        ),
      ],
      onSave: _save,
      onReset: () {
        _titleController.clear();
        _contentController.clear();
      },
    );
  }
}
