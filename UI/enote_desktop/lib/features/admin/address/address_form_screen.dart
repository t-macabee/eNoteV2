import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:enote_core/enote_core.dart';

import '../../../widgets/async_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_form_sections.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import '../city/city_form_screen.dart';
import '../city/city_provider.dart';
import 'address_provider.dart';

class AddressFormScreen extends StatefulWidget {
  final AddressReferenceDto? existing;
  final EntityFormPresentation presentation;
  final bool closeAfterAdd;
  final void Function(AddressReferenceDto created)? onCreated;

  const AddressFormScreen({
    super.key,
    this.existing,
    this.presentation = EntityFormPresentation.page,
    this.closeAfterAdd = false,
    this.onCreated,
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

    final created = await provider.save(
      id: widget.existing?.id,
      request: request.toJson(),
    );
    if (widget.existing == null && created != null) {
      widget.onCreated?.call(created);
    }
    return true;
  }

  Future<CityDto?> _createCity(BuildContext context) async {
    CityDto? created;

    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => CityFormScreen(
        presentation: EntityFormPresentation.dialog,
        closeAfterAdd: true,
        onCreated: (city) => created = city,
      ),
    );

    return created;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      savedMessage: 'Adresa je sačuvana.',
      presentation: widget.presentation,
      title: widget.existing == null ? 'Dodaj adresu' : 'Uredi adresu',
      isEditMode: widget.existing != null,
      deleteDisabledReason: widget.existing?.deleteBlockedReason,
      closeAfterAdd: widget.closeAfterAdd,
      onReset: () {
        clearTextControllers();
        setState(() => _selectedCityId = null);
      },
      fieldsBuilder: (_) => [
        FormSection(
          title: 'Adresa',
          children: [
            AsyncDropdown<CityDto>(
              label: 'Grad',
              value: _selectedCityId,
              fetcher: () => fetchAllPages(
                (query) => context.read<CityProvider>().search(query),
              ),
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
              onCreate: _createCity,
              createTooltip: 'Dodaj grad',
            ),
            FormFieldRow(
              children: [
                EntityTextField(
                  controller: _streetController,
                  label: 'Ulica',
                  maxLength: 100,
                ),
                EntityTextField(
                  controller: _numberController,
                  label: 'Broj',
                  maxLength: 20,
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
