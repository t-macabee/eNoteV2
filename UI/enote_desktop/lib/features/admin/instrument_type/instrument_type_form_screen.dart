import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'instrument_type_provider.dart';

class InstrumentTypeFormScreen extends StatefulWidget {
  final InstrumentTypeDto? existing;

  /// How the wrapped [EntityFormScaffold] is presented — pass
  /// [EntityFormPresentation.dialog] when opened via
  /// [EntityFormScaffold.showAsDialog].
  final EntityFormPresentation presentation;

  const InstrumentTypeFormScreen({
    super.key,
    this.existing,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<InstrumentTypeFormScreen> createState() =>
      _InstrumentTypeFormScreenState();
}

class _InstrumentTypeFormScreenState extends State<InstrumentTypeFormScreen> {
  final _typeController = TextEditingController();
  final _monthlyFeeController = TextEditingController();

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _typeController.text = existing.type;
      _monthlyFeeController.text = existing.monthlyFee.toStringAsFixed(2);
    }
  }

  @override
  void dispose() {
    _typeController.dispose();
    _monthlyFeeController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final provider = context.read<InstrumentTypeProvider>();
    final rawFee = _monthlyFeeController.text.trim().replaceAll(',', '.');
    final monthlyFee = double.parse(rawFee);
    final request = InstrumentTypeRequest(
      type: _typeController.text.trim(),
      monthlyFee: monthlyFee,
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
      presentation: widget.presentation,
      title: widget.existing == null
          ? 'Dodaj tip instrumenta'
          : 'Uredi tip instrumenta',
      isEditMode: widget.existing != null,
      onReset: () {
        _typeController.clear();
        _monthlyFeeController.clear();
      },
      fieldsBuilder: (_) => [
        TextFormField(
          controller: _typeController,
          decoration: const InputDecoration(labelText: 'Tip'),
          validator: Validators.required('Tip'),
        ),
        TextFormField(
          controller: _monthlyFeeController,
          decoration: const InputDecoration(
            labelText: 'Mjesečna naknada',
            hintText: 'npr. 25.00',
          ),
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [
            FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
          ],
          validator: Validators.nonNegativeDecimal,
        ),
      ],
      onSave: _save,
    );
  }
}
