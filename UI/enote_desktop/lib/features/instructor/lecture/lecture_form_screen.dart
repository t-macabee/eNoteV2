import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/date_time_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'lecture_provider.dart';
import 'lecture_type_label.dart';

class LectureFormScreen extends StatefulWidget {
  final int courseId;
  final LectureDto? existing;

  const LectureFormScreen({
    super.key,
    required this.courseId,
    this.existing,
  });

  @override
  State<LectureFormScreen> createState() => _LectureFormScreenState();
}

class _LectureFormScreenState extends State<LectureFormScreen> {
  final _nameController = TextEditingController();
  final _locationController = TextEditingController();
  final _durationController = TextEditingController();
  final _capacityController = TextEditingController();

  LectureType _lectureType = LectureType.theoretical;
  DateTime? _lectureTime;

  bool get _isCancelled => widget.existing?.isCancelled ?? false;
  bool get _isEditMode => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _nameController.text = existing.name;
      _locationController.text = existing.location;
      _lectureType = existing.lectureType;
      _lectureTime = existing.lectureTime;
      _durationController.text = existing.duration.toString();
      _capacityController.text = existing.capacity?.toString() ?? '';
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _locationController.dispose();
    _durationController.dispose();
    _capacityController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    if (_isCancelled) return false;

    if (_lectureTime == null) {
      ErrorBanner.show(context, message: 'Vrijeme predavanja je obavezno.');
      return false;
    }

    final durationText = _durationController.text.trim();
    final duration = int.tryParse(durationText);
    if (duration == null || duration <= 0) {
      ErrorBanner.show(context, message: 'Trajanje mora biti pozitivan broj (minute).');
      return false;
    }

    int? capacity;
    final capacityText = _capacityController.text.trim();
    if (capacityText.isNotEmpty) {
      capacity = int.tryParse(capacityText);
      if (capacity == null || capacity < 0) {
        ErrorBanner.show(context, message: 'Kapacitet mora biti nenegativan broj.');
        return false;
      }
    }

    final provider = context.read<LectureProvider>();
    try {
      if (!_isEditMode) {
        final request = LectureCreateRequest(
          name: _nameController.text.trim(),
          location: _locationController.text.trim(),
          lectureType: _lectureType,
          lectureTime: _lectureTime!,
          duration: duration,
          capacity: capacity,
          courseId: widget.courseId,
        );
        await provider.insert(request.toJson());
      } else {
        final request = LectureUpdateRequest(
          name: _nameController.text.trim(),
          location: _locationController.text.trim(),
          lectureTime: _lectureTime!,
          duration: duration,
          capacity: capacity,
        );
        await provider.update(widget.existing!.id, request.toJson());
      }
      return true;
    } catch (e) {
      // Re-throw so EntityFormScaffold's catch shows it via ErrorBanner
      rethrow;
    }
  }

  @override
  Widget build(BuildContext context) {
    final enabled = !_isCancelled;

    return EntityFormScaffold(
      title: _isEditMode ? 'Uredi predavanje' : 'Dodaj predavanje',
      isEditMode: _isEditMode,
      fieldsBuilder: (_) => [
        if (_isCancelled) ...[
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.orange.shade50,
              border: Border.all(color: Colors.orange.shade200),
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Row(
              children: [
                Icon(Icons.warning_amber, color: Colors.orange),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Ovo predavanje je otkazano i ne može se uređivati.',
                    style: TextStyle(color: Colors.orange),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
        ],
        TextFormField(
          controller: _nameController,
          decoration: const InputDecoration(labelText: 'Naziv'),
          enabled: enabled,
          validator: enabled ? Validators.required('Naziv') : null,
        ),
        TextFormField(
          controller: _locationController,
          decoration: const InputDecoration(labelText: 'Lokacija'),
          enabled: enabled,
          validator: enabled ? Validators.required('Lokacija') : null,
        ),
        const SizedBox(height: 8),
        DropdownButtonFormField<LectureType>(
          initialValue: _lectureType,
          decoration: const InputDecoration(
            labelText: 'Tip predavanja',
            border: OutlineInputBorder(),
          ),
          items: LectureType.values
              .map((t) => DropdownMenuItem(
                    value: t,
                    child: Text(lectureTypeLabel(t)),
                  ))
              .toList(),
          onChanged: enabled && !_isEditMode
              ? (value) {
                  if (value != null) setState(() => _lectureType = value);
                }
              : null,
          validator: enabled
              ? (value) => value == null ? 'Tip je obavezan.' : null
              : null,
        ),
        const SizedBox(height: 8),
        DateTimeField(
          labelText: 'Vrijeme predavanja',
          initialValue: _lectureTime,
          enabled: enabled,
          onChanged: enabled ? (value) => _lectureTime = value : null,
          validator: enabled
              ? (value) => value == null ? 'Vrijeme je obavezno.' : null
              : null,
        ),
        const SizedBox(height: 8),
        TextFormField(
          controller: _durationController,
          decoration: const InputDecoration(
            labelText: 'Trajanje (minute)',
            hintText: 'npr. 90',
          ),
          enabled: enabled,
          keyboardType: TextInputType.number,
          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
          validator: enabled ? _validateDuration : null,
        ),
        TextFormField(
          controller: _capacityController,
          decoration: const InputDecoration(
            labelText: 'Kapacitet (opcionalno)',
            hintText: 'ostavite prazno za neograničeno',
          ),
          enabled: enabled,
          keyboardType: TextInputType.number,
          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
          validator: enabled ? _validateCapacity : null,
        ),
      ],
      onSave: _save,
      onReset: () => setState(() => _lectureTime = null),
    );
  }

  String? _validateDuration(String? value) {
    if (value == null || value.trim().isEmpty) return 'Trajanje je obavezno.';
    final parsed = int.tryParse(value.trim());
    if (parsed == null || parsed <= 0) return 'Unesite pozitivan broj.';
    return null;
  }

  String? _validateCapacity(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final parsed = int.tryParse(value.trim());
    if (parsed == null || parsed < 0) return 'Unesite nenegativan broj.';
    return null;
  }
}

