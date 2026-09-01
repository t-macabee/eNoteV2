import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/date_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../instructor/instructor_provider.dart';
import 'admin_course_provider.dart';

/// Create-only admin course form: admins can create a course on an
/// instructor's behalf (e.g. instructor locked out of their account) and
/// assign it to a specific instructor via the [AsyncDropdown]. There is no
/// edit path — instructor course update stays Instructor-owned.
class AdminCourseFormScreen extends StatefulWidget {
  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const AdminCourseFormScreen({
    super.key,
    this.presentation = EntityFormPresentation.dialog,
  });

  @override
  State<AdminCourseFormScreen> createState() => _AdminCourseFormScreenState();
}

class _AdminCourseFormScreenState extends State<AdminCourseFormScreen> {
  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _priceController = TextEditingController();

  DateTime? _startDate;
  DateTime? _endDate;
  int? _instructorId;
  bool _isPublished = false;

  // Bumped on every reset so the AsyncDropdown below remounts instead of
  // keeping its stale internal selection. DropdownButtonFormField's
  // `initialValue` only applies on first build — EntityFormScaffold calls
  // FormState.reset() *before* onReset() clears _instructorId, so without a
  // changing key the dropdown would keep showing the previous instructor
  // (while _instructorId, the value actually sent, is already null).
  int _formGeneration = 0;

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
      instructorId: _instructorId,
    );

    await context.read<AdminCourseProvider>().insert(request.toJson());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: 'Dodaj kurs',
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
        const SizedBox(height: 8),
        AsyncDropdown<InstructorDto>(
          key: ValueKey(_formGeneration),
          label: 'Instruktor',
          fetcher: () async {
            final result = await context.read<InstructorProvider>().search({
              'page': 1,
              'pageSize': 200,
              'includeTotalCount': false,
            });
            return result.items;
          },
          itemLabel: (instructor) {
            final name =
                '${instructor.firstName ?? ''} ${instructor.lastName ?? ''}'
                    .trim();
            return name.isNotEmpty ? name : (instructor.username ?? '-');
          },
          itemId: (instructor) => instructor.id,
          value: _instructorId,
          onChanged: (id, _) => setState(() => _instructorId = id as int?),
          validator: (value) => value == null ? 'Instruktor je obavezan.' : null,
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
          _instructorId = null;
          _isPublished = false;
          _formGeneration++;
        });
      },
    );
  }
}
