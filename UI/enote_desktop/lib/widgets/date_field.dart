import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

Widget buildSharedDateField({
  required FormFieldState<DateTime?> state,
  required String labelText,
  required String? hintText,
  required bool enabled,
  required ValueChanged<DateTime?>? onChanged,
  required DateTime? firstDate,
  required DateTime? lastDate,
  required Future<DateTime?> Function(BuildContext, DateTime, DateTime, DateTime) pickCallback,
  required IconData suffixIcon,
  required String tooltip,
  required String defaultHint,
  required String Function(DateTime) formatFunction,
}) {
  final value = state.value;
  final text = value != null ? formatFunction(value) : '';

  Future<void> handlePick() async {
    final context = state.context;
    final now = DateTime.now();
    final initialDate = value ?? now;
    final first = firstDate ?? DateTime(2000);
    final last = lastDate ?? DateTime(2100);
    final clampedInitial = initialDate.isBefore(first)
        ? first
        : initialDate.isAfter(last)
        ? last
        : initialDate;

    final picked = await pickCallback(context, clampedInitial, first, last);
    if (picked == null) return;

    state.didChange(picked);
    onChanged?.call(picked);
  }

  return InputDecorator(
    isEmpty: value == null,
    decoration: InputDecoration(
      labelText: labelText,
      hintText: hintText ?? defaultHint,
      border: const OutlineInputBorder(),
      errorText: state.errorText,
      suffixIcon: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (value != null && enabled)
            IconButton(
              icon: const Icon(Icons.clear, size: 18),
              tooltip: 'Obriši',
              onPressed: () {
                state.didChange(null);
                onChanged?.call(null);
              },
            ),
          IconButton(
            icon: Icon(suffixIcon, size: 18),
            tooltip: tooltip,
            onPressed: enabled ? handlePick : null,
          ),
        ],
      ),
    ),
    child: InkWell(
      onTap: enabled ? handlePick : null,
      child: Text(
        text.isEmpty ? '' : text,
        style: TextStyle(
          color: text.isEmpty ? Theme.of(state.context).hintColor : null,
        ),
      ),
    ),
  );
}

class DateField extends FormField<DateTime?> {
  DateField({
    super.key,
    required String labelText,
    super.initialValue,
    super.validator,
    ValueChanged<DateTime?>? onChanged,
    String? hintText,
    bool enabled = true,
    DateTime? firstDate,
    DateTime? lastDate,
    super.autovalidateMode,
  }) : super(
         builder: (FormFieldState<DateTime?> state) {
           return buildSharedDateField(
             state: state,
             labelText: labelText,
             hintText: hintText,
             enabled: enabled,
             onChanged: onChanged,
             firstDate: firstDate,
             lastDate: lastDate,
             pickCallback: (context, clampedInitial, first, last) {
               return showDatePicker(
                 context: context,
                 initialDate: clampedInitial,
                 firstDate: first,
                 lastDate: last,
               );
             },
             suffixIcon: Icons.calendar_today,
             tooltip: 'Odaberi datum',
             defaultHint: 'dd.MM.yyyy.',
             formatFunction: formatDate,
           );
         },
       );
}
