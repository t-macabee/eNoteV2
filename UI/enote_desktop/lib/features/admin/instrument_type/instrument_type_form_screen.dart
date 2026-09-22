import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_text_field.dart';
import '../../../widgets/form_controller_lifecycle.dart';
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

class _InstrumentTypeFormScreenState extends State<InstrumentTypeFormScreen>
    with FormControllerLifecycle {
  late final _typeController = textController();
  late final _monthlyFeeController = textController();

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _typeController.text = existing.type;
      _monthlyFeeController.text = existing.monthlyFee.toStringAsFixed(2);
    }
  }

  Future<bool> _save() async {
    final provider = context.read<InstrumentTypeProvider>();
    final rawFee = _monthlyFeeController.text.trim().replaceAll(',', '.');
    final monthlyFee = double.parse(rawFee);
    final request = InstrumentTypeRequest(
      type: _typeController.text.trim(),
      monthlyFee: monthlyFee,
    );

    await provider.save(id: widget.existing?.id, request: request.toJson());
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
      onReset: clearTextControllers,
      fieldsBuilder: (_) => [
        EntityTextField(controller: _typeController, label: 'Tip'),
        EntityTextField(
          controller: _monthlyFeeController,
          label: 'Mjesečna naknada',
          hintText: 'npr. 25.00',
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [
            FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]')),
          ],
          validator: (value) => Validators.nonNegativeDecimal(value, max: 99999999.99),
        ),
      ],
      onSave: _save,
    );
  }
}
