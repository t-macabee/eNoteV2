import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/date_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'course_provider.dart';

class CourseFormScreen extends StatefulWidget {
  final CourseDto? existing;

  const CourseFormScreen({super.key, this.existing});

  @override
  State<CourseFormScreen> createState() => _CourseFormScreenState();
}

class _CourseFormScreenState extends State<CourseFormScreen> {
  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _priceController = TextEditingController();

  DateTime? _startDate;
  DateTime? _endDate;
  bool _isPublished = false;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _nameController.text = existing.name;
      _descriptionController.text = existing.description ?? '';
      _priceController.text = existing.price.toStringAsFixed(2);
      _startDate = existing.startDate;
      _endDate = existing.endDate;
      _isPublished = existing.isPublished;
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    _priceController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    // Client-side guard: end date not before start date
    if (_startDate != null && _endDate != null && _endDate!.isBefore(_startDate!)) {
      ErrorBanner.show(
        context,
        message: 'Datum završetka ne može biti prije datuma početka.',
      );
      return false;
    }

    final rawPrice = _priceController.text.trim().replaceAll(',', '.');
    final price = double.tryParse(rawPrice);
    if (price == null) {
      ErrorBanner.show(context, message: 'Unesite važeći broj.');
      return false;
    }

    final descriptionText = _descriptionController.text.trim();
    final request = CourseRequest(
      name: _nameController.text.trim(),
      description: descriptionText.isEmpty ? null : descriptionText,
      price: price,
      startDate: _startDate,
      endDate: _endDate,
      isPublished: _isPublished,
    );

    final provider = context.read<CourseProvider>();
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
      title: widget.existing == null ? 'Dodaj kurs' : 'Uredi kurs',
      isEditMode: widget.existing != null,
      fieldsBuilder: (_) => [
        TextFormField(
          controller: _nameController,
          decoration: const InputDecoration(labelText: 'Naziv'),
          validator: Validators.required('Naziv'),
        ),
        TextFormField(
          controller: _descriptionController,
          decoration: const InputDecoration(labelText: 'Opis'),
          maxLines: 3,
        ),
        TextFormField(
          controller: _priceController,
          decoration: const InputDecoration(
            labelText: 'Cijena',
            hintText: 'npr. 25.00',
          ),
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [
            FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
          ],
          validator: Validators.nonNegativeDecimal,
        ),
        const SizedBox(height: 8),
        DateField(
          labelText: 'Datum početka',
          initialValue: _startDate,
          onChanged: (value) => _startDate = value,
        ),
        const SizedBox(height: 8),
        DateField(
          labelText: 'Datum završetka',
          initialValue: _endDate,
          onChanged: (value) => _endDate = value,
        ),
        const SizedBox(height: 8),
        SwitchListTile(
          title: const Text('Objavljen'),
          value: _isPublished,
          onChanged: (value) => setState(() => _isPublished = value),
        ),
      ],
      onSave: _save,
      onReset: () {
        _nameController.clear();
        _descriptionController.clear();
        _priceController.clear();
        setState(() {
          _startDate = null;
          _endDate = null;
          _isPublished = false;
        });
      },
    );
  }
}
