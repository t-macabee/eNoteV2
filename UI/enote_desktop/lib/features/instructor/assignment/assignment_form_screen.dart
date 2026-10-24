import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/date_time_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import 'assignment_provider.dart';

class AssignmentFormScreen extends StatefulWidget {
  final int lectureId;
  final AssignmentDto? existing;

  final EntityFormPresentation presentation;

  const AssignmentFormScreen({
    super.key,
    required this.lectureId,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<AssignmentFormScreen> createState() => _AssignmentFormScreenState();
}

class _AssignmentFormScreenState extends State<AssignmentFormScreen>
    with FormControllerLifecycle {
  late final _titleController = textController();
  late final _descriptionController = textController();
  final _dueAtFieldKey = GlobalKey<FormFieldState<DateTime?>>();
  DateTime? _dueAt;
  String? _dueAtError;

  bool get _isEditMode => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _titleController.text = existing.title;
      _descriptionController.text = existing.description;
      _dueAt = existing.dueAt;
    }
  }

  Future<bool> _save() async {
    if (_dueAt == null) {
      ErrorBanner.show(context, message: 'Rok je obavezan.');
      return false;
    }

    final provider = context.read<AssignmentProvider>();
    final request = AssignmentRequest(
      title: _titleController.text.trim(),
      description: _descriptionController.text.trim(),
      dueAt: _dueAt!,
    );

    try {
      await provider.save(id: widget.existing?.id, request: request.toJson());
    } on ApiException catch (e) {
      final dueAtError = _isEditMode ? null : e.fieldError('DueAt');
      if (dueAtError == null) rethrow;
      setState(() => _dueAtError = dueAtError);
      _dueAtFieldKey.currentState?.validate();
      return false;
    }
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      savedMessage: 'Zadatak je sačuvan.',
      presentation: widget.presentation,
      title: _isEditMode ? 'Uredi zadatak' : 'Dodaj zadatak',
      isEditMode: _isEditMode,
      closeAfterAdd: true,
      fieldsBuilder: (_) => [
        EntityTextField(controller: _titleController, label: 'Naslov', maxLength: 200),
        EntityTextField(
          controller: _descriptionController,
          label: 'Opis',
          maxLines: 6,
          minLines: 3,
        ),
        DateTimeField(
          key: _dueAtFieldKey,
          labelText: 'Rok',
          initialValue: _dueAt,
          firstDate: _isEditMode ? null : DateTime.now(),
          autovalidateMode: AutovalidateMode.onUserInteraction,
          validator: (value) {
            if (value == null) return 'Rok je obavezan.';
            if (!_isEditMode && !value.isAfter(DateTime.now())) {
              return 'Rok za predaju mora biti u budućnosti.';
            }
            return _dueAtError;
          },
          onChanged: (value) => setState(() {
            _dueAt = value;
            _dueAtError = null;
          }),
        ),
      ],
      onSave: _save,
      onReset: () {
        clearTextControllers();
        setState(() {
          _dueAt = null;
          _dueAtError = null;
        });
      },
    );
  }
}
