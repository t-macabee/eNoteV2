import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'address_provider.dart';

class AddressFormScreen extends StatefulWidget {
  final AddressReferenceDto? existing;

  const AddressFormScreen({super.key, this.existing});

  @override
  State<AddressFormScreen> createState() => _AddressFormScreenState();
}

class _AddressFormScreenState extends State<AddressFormScreen> {
  final _cityController = TextEditingController();
  final _streetController = TextEditingController();
  final _numberController = TextEditingController();

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _cityController.text = existing.city;
      _streetController.text = existing.street;
      _numberController.text = existing.number;
    }
  }

  @override
  void dispose() {
    _cityController.dispose();
    _streetController.dispose();
    _numberController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final provider = context.read<AddressProvider>();
    final request = AddressRequest(
      city: _cityController.text.trim(),
      street: _streetController.text.trim(),
      number: _numberController.text.trim(),
    );

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
      fieldsBuilder: (_) => [
        TextFormField(
          controller: _cityController,
          decoration: const InputDecoration(labelText: 'Grad'),
          validator: Validators.required('Grad'),
        ),
        TextFormField(
          controller: _streetController,
          decoration: const InputDecoration(labelText: 'Ulica'),
          validator: Validators.required('Ulica'),
        ),
        TextFormField(
          controller: _numberController,
          decoration: const InputDecoration(labelText: 'Broj'),
          validator: Validators.required('Broj'),
        ),
      ],
      onSave: _save,
    );
  }
}
