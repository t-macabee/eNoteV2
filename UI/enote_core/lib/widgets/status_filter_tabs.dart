import 'package:flutter/material.dart';

import '../theme/app_tokens.dart';

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

const List<StatusFilterOption<bool?>> activeFilterOptions = [
  StatusFilterOption(label: 'Aktivni', value: true),
  StatusFilterOption(label: 'Neaktivni', value: false),
  StatusFilterOption(label: 'Svi', value: null),
];

const List<StatusFilterOption<bool?>> publishedFilterOptions = [
  StatusFilterOption(label: 'Objavljeni', value: true),
  StatusFilterOption(label: 'Neobjavljeni', value: false),
  StatusFilterOption(label: 'Svi', value: null),
];

class StatusFilterTabs<T> extends StatelessWidget {
  final List<StatusFilterOption<T>> options;
  final T selected;
  final ValueChanged<T> onChanged;
  final bool scrollable;

  const StatusFilterTabs({
    super.key,
    required this.options,
    required this.selected,
    required this.onChanged,
    this.scrollable = false,
  });

  @override
  Widget build(BuildContext context) {
    final chips = [for (final option in options) _buildChip(context, option)];

    if (scrollable) {
      return SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: [
            for (var i = 0; i < chips.length; i++) ...[
              if (i > 0) const SizedBox(width: AppSpacing.sm),
              chips[i],
            ],
          ],
        ),
      );
    }

    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: chips,
    );
  }

  Widget _buildChip(BuildContext context, StatusFilterOption<T> option) {
    final colorScheme = Theme.of(context).colorScheme;
    final isSelected = option.value == selected;

    return ChoiceChip(
      label: Text(
        option.count != null
            ? '${option.label} (${option.count})'
            : option.label,
      ),
      selected: isSelected,
      showCheckmark: false,
      visualDensity: VisualDensity.compact,
      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
      labelStyle: TextStyle(
        color: isSelected
            ? colorScheme.onPrimary
            : colorScheme.onSurfaceVariant,
        fontWeight: isSelected ? FontWeight.w600 : FontWeight.w400,
      ),
      selectedColor: colorScheme.primary,
      backgroundColor: colorScheme.surfaceContainer,
      side: BorderSide(color: colorScheme.outline),
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.xs,
      ),
      onSelected: (_) => onChanged(option.value),
    );
  }
}
