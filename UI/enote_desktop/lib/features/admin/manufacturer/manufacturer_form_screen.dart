import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_form_sections.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import 'manufacturer_provider.dart';

class ManufacturerFormScreen extends StatefulWidget {
  final ManufacturerDto? existing;

  final EntityFormPresentation presentation;

  const ManufacturerFormScreen({
    super.key,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<ManufacturerFormScreen> createState() => _ManufacturerFormScreenState();
}

class _ManufacturerFormScreenState extends State<ManufacturerFormScreen>
    with FormControllerLifecycle {
  late final _nameController = textController();

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _nameController.text = existing.name;
    }
  }

  Future<bool> _save() async {
    final provider = context.read<ManufacturerProvider>();
    final request = ManufacturerRequest(name: _nameController.text.trim());

    await provider.save(id: widget.existing?.id, request: request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      savedMessage: 'Proizvođač je sačuvan.',
      presentation: widget.presentation,
      title: widget.existing == null
          ? 'Dodaj proizvođača'
          : 'Uredi proizvođača',
      isEditMode: widget.existing != null,
      onReset: clearTextControllers,
      fieldsBuilder: (_) => [
        FormSection(
          title: 'Osnovni podaci',
          children: [
            FormFieldRow(
              children: [
                EntityTextField(
                  controller: _nameController,
                  label: 'Naziv',
                  maxLength: 100,
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'Naziv proizvođača je obavezan.';
                    }
                    return null;
                  },
                ),
              ],
            ),
          ],
        ),
      ],
      onSave: _save,
    );
  }
}
