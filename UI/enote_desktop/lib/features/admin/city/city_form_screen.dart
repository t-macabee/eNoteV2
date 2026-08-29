import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'city_provider.dart';

class CityFormScreen extends StatefulWidget {
  final CityDto? existing;

  const CityFormScreen({super.key, this.existing});

  @override
  State<CityFormScreen> createState() => _CityFormScreenState();
}

class _CityFormScreenState extends State<CityFormScreen> {
  final _nameController = TextEditingController();

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _nameController.text = existing.name;
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final provider = context.read<CityProvider>();
    final request = CityRequest(name: _nameController.text.trim());

    if (widget.existing == null) {
      await provider.insert(request.toJson());
    } else {
      await provider.update(widget.existing!.id, request.toJson());
    }
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: widget.existing == null ? 'Dodaj grad' : 'Uredi grad',
      isEditMode: widget.existing != null,
      onReset: () => _nameController.clear(),
      fieldsBuilder: (_) => [
        TextFormField(
          controller: _nameController,
          decoration: const InputDecoration(labelText: 'Naziv grada'),
          validator: Validators.required('Naziv grada'),
        ),
      ],
      onSave: _save,
    );
  }
}
