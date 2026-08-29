import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../city/city_provider.dart';
import 'address_provider.dart';

class AddressFormScreen extends StatefulWidget {
  final AddressReferenceDto? existing;

  const AddressFormScreen({super.key, this.existing});

  @override
  State<AddressFormScreen> createState() => _AddressFormScreenState();
}

class _AddressFormScreenState extends State<AddressFormScreen> {
  final _streetController = TextEditingController();
  final _numberController = TextEditingController();
  int? _selectedCityId;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _selectedCityId = existing.cityId;
      _streetController.text = existing.street;
      _numberController.text = existing.number;
    }
  }

  @override
  void dispose() {
    _streetController.dispose();
    _numberController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final provider = context.read<AddressProvider>();
    final request = AddressRequest(
      cityId: _selectedCityId!,
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
      title: widget.existing == null ? 'Dodaj adresu' : 'Uredi adresu',
      isEditMode: widget.existing != null,
      onReset: () {
        _streetController.clear();
        _numberController.clear();
        setState(() => _selectedCityId = null);
      },
      fieldsBuilder: (_) => [
        AsyncDropdown<CityDto>(
          label: 'Grad',
          value: _selectedCityId,
          fetcher: () async {
            final provider = context.read<CityProvider>();
            final result = await provider.search({
              'page': 1,
              'pageSize': 100,
              'includeTotalCount': true,
            });
            return result.items;
          },
          itemLabel: (item) => item.name,
          itemId: (item) => item.id,
          onChanged: (id, item) {
            setState(() {
              _selectedCityId = id as int?;
            });
          },
          validator: (value) {
            if (value == null) return 'Grad je obavezan';
            return null;
          },
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
