import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/date_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
import 'course_provider.dart';

class CourseFormScreen extends StatefulWidget {
  final CourseDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const CourseFormScreen({
    super.key,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<CourseFormScreen> createState() => _CourseFormScreenState();
}

class _CourseFormScreenState extends State<CourseFormScreen>
    with FormControllerLifecycle {
  late final _nameController = textController();
  late final _descriptionController = textController();
  late final _priceController = textController();

  DateTime? _startDate;
  DateTime? _endDate;

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
    }
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
    // Publishing stays in the detail dialog behind its confirm; the form
    // round-trips the loaded value on edit and sends false on create.
    final request = CourseRequest(
      name: _nameController.text.trim(),
      description: descriptionText.isEmpty ? null : descriptionText,
      price: price,
      startDate: _startDate,
      endDate: _endDate,
      isPublished: widget.existing?.isPublished ?? false,
    );

    final provider = context.read<CourseProvider>();
    await provider.save(id: widget.existing?.id, request: request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: widget.existing == null ? 'Dodaj kurs' : 'Uredi kurs',
      isEditMode: widget.existing != null,
      closeAfterAdd: true,
      fieldsBuilder: (_) => [
        EntityTextField(controller: _nameController, label: 'Naziv'),
        EntityTextField(
          controller: _descriptionController,
          label: 'Opis',
          required: false,
          maxLines: 3,
        ),
        EntityTextField(
          controller: _priceController,
          label: 'Mjesečna cijena',
          hintText: 'npr. 25.00',
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [
            FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
          ],
          validator: (value) =>
              Validators.nonNegativeDecimal(value, max: 10000),
        ),
        DateField(
          labelText: 'Datum početka',
          initialValue: _startDate,
          onChanged: (value) => _startDate = value,
        ),
        DateField(
          labelText: 'Datum završetka',
          initialValue: _endDate,
          onChanged: (value) => _endDate = value,
        ),
      ],
      onSave: _save,
      onReset: () {
        clearTextControllers();
        setState(() {
          _startDate = null;
          _endDate = null;
        });
      },
    );
  }
}
