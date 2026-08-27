import 'package:flutter/material.dart';

String _formatDateTime(DateTime d) {
  final day = d.day.toString().padLeft(2, '0');
  final month = d.month.toString().padLeft(2, '0');
  final hour = d.hour.toString().padLeft(2, '0');
  final minute = d.minute.toString().padLeft(2, '0');
  return '$day.$month.${d.year}. $hour:$minute';
}

/// Date+time picker form field — same [FormField<DateTime?>] shape as
/// [DateField] but composes [showDatePicker] + [showTimePicker] since
/// `LectureTime` carries both date and time.
///
/// Display format `dd.MM.yyyy. HH:mm`, same clear/validate contract as
/// [DateField] for visual and behavioral consistency.
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
            final value = state.value;
            final text = value != null ? _formatDateTime(value) : '';

            Future<void> pickDateTime() async {
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

              final pickedDate = await showDatePicker(
                context: context,
                initialDate: clampedInitial,
                firstDate: first,
                lastDate: last,
              );
              if (pickedDate == null) return;
              if (!context.mounted) return;

              final initialTime = TimeOfDay.fromDateTime(value ?? pickedDate);
              final pickedTime = await showTimePicker(
                context: context,
                initialTime: initialTime,
              );
              if (pickedTime == null) return;

              final combined = DateTime(
                pickedDate.year,
                pickedDate.month,
                pickedDate.day,
                pickedTime.hour,
                pickedTime.minute,
              );
              state.didChange(combined);
              onChanged?.call(combined);
            }

            return InputDecorator(
              isEmpty: value == null,
              decoration: InputDecoration(
                labelText: labelText,
                hintText: hintText ?? 'dd.MM.yyyy. HH:mm',
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
                      tooltip: 'Odaberi datum i vrijeme',
                      onPressed: enabled ? pickDateTime : null,
                    ),
                  ],
                ),
              ),
              child: InkWell(
                onTap: enabled ? pickDateTime : null,
                child: Text(
                  text.isEmpty ? '' : text,
                  style: TextStyle(
                    color: text.isEmpty ? Theme.of(state.context).hintColor : null,
                  ),
                ),
              ),
            );
          },
        );
}
