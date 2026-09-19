import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
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

class _LectureNoteFormScreenState extends State<LectureNoteFormScreen>
    with FormControllerLifecycle {
  late final _titleController = textController();
  late final _contentController = textController();

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

  Future<bool> _save() async {
    final provider = context.read<LectureNoteProvider>();
    final request = LectureNoteRequest(
      title: _titleController.text.trim(),
      content: _contentController.text.trim(),
    );

    await provider.save(id: widget.existing?.id, request: request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: _isEditMode ? 'Uredi bilješku' : 'Dodaj bilješku',
      isEditMode: _isEditMode,
      fieldsBuilder: (_) => [
        EntityTextField(controller: _titleController, label: 'Naslov'),
        EntityTextField(
          controller: _contentController,
          label: 'Sadržaj',
          maxLines: 8,
          minLines: 4,
        ),
      ],
      onSave: _save,
      onReset: clearTextControllers,
    );
  }
}
