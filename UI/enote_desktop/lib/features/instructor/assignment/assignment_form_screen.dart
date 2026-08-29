import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/date_time_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'assignment_provider.dart';

class AssignmentFormScreen extends StatefulWidget {
  final int lectureId;
  final AssignmentDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
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

class _AssignmentFormScreenState extends State<AssignmentFormScreen> {
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  DateTime? _dueAt;

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

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    super.dispose();
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
      title: _isEditMode ? 'Uredi zadatak' : 'Dodaj zadatak',
      isEditMode: _isEditMode,
      fieldsBuilder: (_) => [
        TextFormField(
          controller: _titleController,
          decoration: const InputDecoration(labelText: 'Naslov'),
          validator: Validators.required('Naslov'),
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: _descriptionController,
          decoration: const InputDecoration(labelText: 'Opis'),
          maxLines: 6,
          minLines: 3,
          validator: Validators.required('Opis'),
        ),
        const SizedBox(height: 12),
        DateTimeField(
          labelText: 'Rok',
          initialValue: _dueAt,
          validator: (value) => value == null ? 'Rok je obavezan.' : null,
          onChanged: (value) => setState(() => _dueAt = value),
        ),
      ],
      onSave: _save,
      onReset: () {
        _titleController.clear();
        _descriptionController.clear();
        setState(() => _dueAt = null);
      },
    );
  }
}
