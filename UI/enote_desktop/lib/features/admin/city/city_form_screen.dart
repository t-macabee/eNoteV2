import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import 'city_provider.dart';

class CityFormScreen extends StatefulWidget {
  final CityDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const CityFormScreen({
    super.key,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<CityFormScreen> createState() => _CityFormScreenState();
}

class _CityFormScreenState extends State<CityFormScreen>
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
    final provider = context.read<CityProvider>();
    final request = CityRequest(name: _nameController.text.trim());

    await provider.save(id: widget.existing?.id, request: request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: widget.existing == null ? 'Dodaj grad' : 'Uredi grad',
      isEditMode: widget.existing != null,
      onReset: clearTextControllers,
      fieldsBuilder: (_) => [
        EntityTextField(controller: _nameController, label: 'Naziv grada'),
      ],
      onSave: _save,
    );
  }
}
