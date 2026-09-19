import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:enote_core/enote_core.dart';

import '../../../widgets/async_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import '../city/city_provider.dart';
import 'address_provider.dart';

class AddressFormScreen extends StatefulWidget {
  final AddressReferenceDto? existing;
  final EntityFormPresentation presentation;

  const AddressFormScreen({
    super.key,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<AddressFormScreen> createState() => _AddressFormScreenState();
}

class _AddressFormScreenState extends State<AddressFormScreen>
    with FormControllerLifecycle {
  late final _streetController = textController();
  late final _numberController = textController();
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

  Future<bool> _save() async {
    final provider = context.read<AddressProvider>();
    final request = AddressRequest(
      cityId: _selectedCityId!,
      street: _streetController.text.trim(),
      number: _numberController.text.trim(),
    );

    await provider.save(id: widget.existing?.id, request: request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: widget.existing == null ? 'Dodaj adresu' : 'Uredi adresu',
      isEditMode: widget.existing != null,
      onReset: () {
        clearTextControllers();
        setState(() => _selectedCityId = null);
      },
      fieldsBuilder: (_) => [
        AsyncDropdown<CityDto>(
          label: 'Grad',
          value: _selectedCityId,
          fetcher: () async {
            final provider = context.read<CityProvider>();
            final result = await provider.search(pagedQuery(1, 100, ''));
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
        EntityTextField(controller: _streetController, label: 'Ulica'),
        EntityTextField(controller: _numberController, label: 'Broj'),
      ],
      onSave: _save,
    );
  }
}
