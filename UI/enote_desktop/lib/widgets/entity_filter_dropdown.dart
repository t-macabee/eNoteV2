import 'package:flutter/material.dart';

class EntityFilterDropdown<T> extends StatelessWidget {
  final String label;
  final List<DropdownMenuItem<T>> items;
  final T? value;
  final ValueChanged<T?>? onChanged;
  final double width;
  final bool isExpanded;
  final bool outlined;

  const EntityFilterDropdown({
    super.key,
    required this.label,
    required this.items,
    required this.value,
    required this.onChanged,
    this.width = 220,
    this.isExpanded = true,
    this.outlined = false,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: width,
      child: DropdownButtonFormField<T>(
        key: ValueKey(value),
        isExpanded: isExpanded,
        initialValue: value,
        decoration: InputDecoration(
          labelText: label,
          border: outlined ? const OutlineInputBorder() : null,
        ),
        items: items,
        onChanged: onChanged,
      ),
    );
  }
}
