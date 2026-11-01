import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class StatusFilterOption<T> {
  final String label;
  final T value;
  final int? count;

  const StatusFilterOption({
    required this.label,
    required this.value,
    this.count,
  });
}

class StatusFilterTabs<T> extends StatelessWidget {
  final List<StatusFilterOption<T>> options;
  final T selected;
  final ValueChanged<T> onChanged;

  const StatusFilterTabs({
    super.key,
    required this.options,
    required this.selected,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        for (final option in options)
          ChoiceChip(
            label: Text(
              option.count != null
                  ? '${option.label} (${option.count})'
                  : option.label,
            ),
            selected: option.value == selected,
            showCheckmark: false,
            visualDensity: VisualDensity.compact,
            materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
            labelStyle: TextStyle(
              color: option.value == selected
                  ? colorScheme.onPrimary
                  : colorScheme.onSurfaceVariant,
              fontWeight: option.value == selected
                  ? FontWeight.w600
                  : FontWeight.w400,
            ),
            selectedColor: colorScheme.primary,
            backgroundColor: colorScheme.surfaceContainer,
            side: BorderSide(color: colorScheme.outline),
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.md,
              vertical: AppSpacing.xs,
            ),
            onSelected: (_) => onChanged(option.value),
          ),
      ],
    );
  }
}
