import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/date_time_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import '../address/address_provider.dart';
import 'event_provider.dart';

const _scopedReason =
    'Događaj pripada kursu. Administrator može mijenjati i brisati samo događaje na nivou platforme.';

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

class _EventFormScreenState extends State<EventFormScreen>
    with FormControllerLifecycle {
  late final _titleController = textController();
  late final _descriptionController = textController();

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
    await provider.save(id: widget.existing?.id, request: request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    final existing = widget.existing;
    final readOnly = existing?.isScoped ?? false;

    return EntityFormScaffold(
      presentation: widget.presentation,
      title: widget.existing == null
          ? 'Dodaj događaj'
          : (readOnly ? 'Pregled događaja' : 'Uredi događaj'),
      isEditMode: widget.existing != null,
      saveEnabled: !readOnly,
      onDelete: existing != null && !readOnly
          ? () async {
              await context.read<EventProvider>().remove(existing.id);
              return true;
            }
          : null,
      deleteDisabledReason: readOnly ? _scopedReason : null,
      fieldsBuilder: (_) => [
        if (readOnly) ...[
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.orange.shade50,
              border: Border.all(color: Colors.orange.shade200),
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Row(
              children: [
                Icon(Icons.info_outline, color: Colors.orange),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _scopedReason,
                    style: TextStyle(color: Colors.orange),
                  ),
                ),
              ],
            ),
          ),
        ],
        EntityTextField(
          controller: _titleController,
          label: 'Naziv',
          enabled: !readOnly,
          required: !readOnly,
        ),
        EntityTextField(
          controller: _descriptionController,
          label: 'Opis',
          maxLines: 3,
          enabled: !readOnly,
          required: !readOnly,
        ),
        DateTimeField(
          labelText: 'Početak',
          initialValue: _startsAt,
          onChanged: (value) => _startsAt = value,
          enabled: !readOnly,
          validator: readOnly
              ? null
              : (value) => value == null ? 'Početak je obavezan.' : null,
        ),
        DateTimeField(
          labelText: 'Kraj',
          initialValue: _endsAt,
          onChanged: (value) => _endsAt = value,
          enabled: !readOnly,
        ),
        AsyncDropdown<AddressReferenceDto>(
          label: 'Adresa',
          fetcher: () async {
            final result = await context.read<AddressProvider>().search({
              'page': 1,
              'pageSize': 200,
            });
            return result.items;
          },
          itemLabel: (address) =>
              '${address.street} ${address.number}, ${address.city}',
          itemId: (address) => address.id,
          value: _addressId,
          onChanged: (id, _) => setState(() => _addressId = id as int?),
          enabled: !readOnly,
        ),
      ],
      onSave: _save,
      onReset: () {
        clearTextControllers();
        setState(() {
          _startsAt = null;
          _endsAt = null;
          _addressId = null;
        });
      },
    );
  }
}
