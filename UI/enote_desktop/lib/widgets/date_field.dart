import 'package:flutter/material.dart';

String _formatDate(DateTime d) {
  final day = d.day.toString().padLeft(2, '0');
  final month = d.month.toString().padLeft(2, '0');
  return '$day.$month.${d.year}.';
}

/// Reusable date-picker form field shaped like a [TextFormField].
///
/// Opens [showDatePicker] on tap and displays the selected date formatted as
/// `dd.MM.yyyy.`. Supports clearing the value via the trailing clear icon.
/// Validates through [FormField.validator] just like a normal form field.
///
/// This is the shared piece for the Instructor → Course module and will be
/// extended with a date+time variant for Lecture's `LectureTime` immediately
/// after this module.
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
            final text = value != null ? _formatDate(value) : '';

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
                      onPressed: enabled
                          ? () async {
                              final context = state.context;
                              final now = DateTime.now();
                              final initialDate = value ?? now;
                              final first = firstDate ?? DateTime(2000);
                              final last = lastDate ?? DateTime(2100);
                              final picked = await showDatePicker(
                                context: context,
                                initialDate: initialDate.isBefore(first)
                                    ? first
                                    : initialDate.isAfter(last)
                                        ? last
                                        : initialDate,
                                firstDate: first,
                                lastDate: last,
                              );
                              if (picked != null) {
                                state.didChange(picked);
                                onChanged?.call(picked);
                              }
                            }
                          : null,
                    ),
                  ],
                ),
              ),
              child: InkWell(
                onTap: enabled
                    ? () async {
                        final context = state.context;
                        final now = DateTime.now();
                        final initialDate = value ?? now;
                        final first = firstDate ?? DateTime(2000);
                        final last = lastDate ?? DateTime(2100);
                        final picked = await showDatePicker(
                          context: context,
                          initialDate: initialDate.isBefore(first)
                              ? first
                              : initialDate.isAfter(last)
                                  ? last
                                  : initialDate,
                          firstDate: first,
                          lastDate: last,
                        );
                        if (picked != null) {
                          state.didChange(picked);
                          onChanged?.call(picked);
                        }
                      }
                    : null,
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
