import 'package:enote_core/enote_core.dart';
import 'package:flutter/material.dart';

import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';

class AnnouncementFormScreen extends StatefulWidget {
  final CrudProvider<AnnouncementDto> provider;
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

class _AnnouncementFormScreenState extends State<AnnouncementFormScreen>
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
    final request = AnnouncementRequest(
      title: _titleController.text.trim(),
      content: _contentController.text.trim(),
    );

    await widget.provider.save(
      id: widget.existing?.id,
      request: request.toJson(),
    );
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
