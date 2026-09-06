import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/date_time_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../address/address_provider.dart';
import 'event_provider.dart';

/// Create/edit form for `admin/events`. Admin can only manage platform-wide
/// events (without course or instructor scoping).
class EventFormScreen extends StatefulWidget {
  final EventDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const EventFormScreen({
    super.key,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<EventFormScreen> createState() => _EventFormScreenState();
}

class _EventFormScreenState extends State<EventFormScreen> {
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();

  DateTime? _startsAt;
  DateTime? _endsAt;
  int? _addressId;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _titleController.text = existing.title;
      _descriptionController.text = existing.description;
      _startsAt = existing.startsAt;
      _endsAt = existing.endsAt;
      _addressId = existing.addressId;
    }
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    if (_startsAt == null) {
      ErrorBanner.show(context, message: 'Datum i vrijeme početka su obavezni.');
      return false;
    }
    if (_endsAt != null && _endsAt!.isBefore(_startsAt!)) {
      ErrorBanner.show(
        context,
        message: 'Vrijeme završetka ne može biti prije vremena početka.',
      );
      return false;
    }

    final request = EventRequest(
      title: _titleController.text.trim(),
      description: _descriptionController.text.trim(),
      startsAt: _startsAt!,
      endsAt: _endsAt,
      addressId: _addressId,
    );

    final provider = context.read<EventProvider>();
    if (widget.existing == null) {
      await provider.insert(request.toJson());
    } else {
      await provider.update(widget.existing!.id, request.toJson());
    }
    return true;
  }

  @override
  Widget build(BuildContext context) {
    final existing = widget.existing;
    final canDelete = existing != null && !existing.isScoped;

    return EntityFormScaffold(
      presentation: widget.presentation,
      title: widget.existing == null ? 'Dodaj događaj' : 'Uredi događaj',
      isEditMode: widget.existing != null,
      onDelete: canDelete
          ? () async {
              await context.read<EventProvider>().remove(existing.id);
              return true;
            }
          : null,
      fieldsBuilder: (_) => [
        TextFormField(
          controller: _titleController,
          decoration: const InputDecoration(labelText: 'Naziv'),
          validator: Validators.required('Naziv'),
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _descriptionController,
          decoration: const InputDecoration(labelText: 'Opis'),
          maxLines: 3,
          validator: Validators.required('Opis'),
        ),
        const SizedBox(height: 16),
        DateTimeField(
          labelText: 'Početak',
          initialValue: _startsAt,
          onChanged: (value) => _startsAt = value,
          validator: (value) => value == null ? 'Početak je obavezan.' : null,
        ),
        const SizedBox(height: 16),
        DateTimeField(
          labelText: 'Kraj',
          initialValue: _endsAt,
          onChanged: (value) => _endsAt = value,
        ),
        const SizedBox(height: 16),
        AsyncDropdown<AddressReferenceDto>(
          label: 'Adresa',
          fetcher: () async {
            final result = await context.read<AddressProvider>().search({
              'page': 1,
              'pageSize': 200,
              'includeTotalCount': false,
            });
            return result.items;
          },
          itemLabel: (address) =>
              '${address.street} ${address.number}, ${address.city}',
          itemId: (address) => address.id,
          value: _addressId,
          onChanged: (id, _) => setState(() => _addressId = id as int?),
        ),
      ],
      onSave: _save,
      onReset: () {
        _titleController.clear();
        _descriptionController.clear();
        setState(() {
          _startsAt = null;
          _endsAt = null;
          _addressId = null;
        });
      },
    );
  }
}
