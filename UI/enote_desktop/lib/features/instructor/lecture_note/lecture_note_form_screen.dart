import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'lecture_note_provider.dart';

class LectureNoteFormScreen extends StatefulWidget {
  final int lectureId;
  final LectureNoteDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const LectureNoteFormScreen({
    super.key,
    required this.lectureId,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<LectureNoteFormScreen> createState() => _LectureNoteFormScreenState();
}

class _LectureNoteFormScreenState extends State<LectureNoteFormScreen> {
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
    final provider = context.read<LectureNoteProvider>();
    final request = LectureNoteRequest(
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
      title: _isEditMode ? 'Uredi bilješku' : 'Dodaj bilješku',
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
      ],
      onSave: _save,
      onReset: () {
        _titleController.clear();
        _contentController.clear();
      },
    );
  }
}
