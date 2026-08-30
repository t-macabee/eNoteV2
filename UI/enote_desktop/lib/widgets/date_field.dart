import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

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
           final value = state.value;
           final text = value != null ? formatDate(value) : '';

           Future<void> pickDate() async {
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

             final picked = await showDatePicker(
               context: context,
               initialDate: clampedInitial,
               firstDate: first,
               lastDate: last,
             );
             if (picked == null) return;

             state.didChange(picked);
             onChanged?.call(picked);
           }

           return InputDecorator(
             isEmpty: value == null,
             decoration: InputDecoration(
               labelText: labelText,
               hintText: hintText ?? 'dd.MM.yyyy.',
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
                     icon: const Icon(Icons.calendar_today, size: 18),
                     tooltip: 'Odaberi datum',
                     onPressed: enabled ? pickDate : null,
                   ),
                 ],
               ),
             ),
             child: InkWell(
               onTap: enabled ? pickDate : null,
               child: Text(
                 text.isEmpty ? '' : text,
                 style: TextStyle(
                   color: text.isEmpty
                       ? Theme.of(state.context).hintColor
                       : null,
                 ),
               ),
             ),
           );
         },
       );
}
