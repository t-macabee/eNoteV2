import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';
import 'date_field.dart';
/// Like `DateField`, but picks a date and a time — for fields such as
/// `Event.StartsAt`/`EndsAt` that carry a time component `DateField` (date
/// only) can't represent.
class DateTimeField extends FormField<DateTime?> {
  DateTimeField({
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
             pickCallback: (context, clampedInitial, first, last) async {
               final pickedDate = await showDatePicker(
                 context: context,
                 initialDate: clampedInitial,
                 firstDate: first,
                 lastDate: last,
               );
               if (pickedDate == null) return null;
               if (!context.mounted) return null;

               final pickedTime = await showTimePicker(
                 context: context,
                 initialTime: TimeOfDay.fromDateTime(clampedInitial),
               );
               if (pickedTime == null) return null;

               return DateTime(
                 pickedDate.year,
                 pickedDate.month,
                 pickedDate.day,
                 pickedTime.hour,
                 pickedTime.minute,
               );
             },
             suffixIcon: Icons.event,
             tooltip: 'Odaberi datum i vrijeme',
             defaultHint: 'dd.MM.yyyy. HH:mm',
             formatFunction: formatDateTime,
           );
         },
       );
}
