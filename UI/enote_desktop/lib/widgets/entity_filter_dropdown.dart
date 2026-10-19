import 'package:flutter/material.dart';

class EntityFilterDropdown<T> extends StatelessWidget {
  final String label;
  final List<DropdownMenuItem<T>> items;
  final T? value;
  final ValueChanged<T?>? onChanged;
  final double width;
  final bool isExpanded;
  final bool outlined;

  final bool isLoading;

  final String? errorText;
  final VoidCallback? onRetry;

  const EntityFilterDropdown({
    super.key,
    required this.label,
    required this.items,
    required this.value,
    required this.onChanged,
    this.width = 220,
    this.isExpanded = true,
    this.outlined = false,
    this.isLoading = false,
    this.errorText,
    this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    if (errorText != null) {
      return SizedBox(
        width: width,
        child: InputDecorator(
          decoration: InputDecoration(
            labelText: label,
            border: const OutlineInputBorder(),
            errorText: errorText,
            suffixIcon: IconButton(
              icon: const Icon(Icons.refresh),
              tooltip: 'Pokušaj ponovo',
              onPressed: onRetry,
            ),
          ),
          child: const Text('-'),
        ),
      );
    }

    if (isLoading) {
      return SizedBox(
        width: width,
        child: InputDecorator(
          decoration: InputDecoration(
            labelText: label,
            border: const OutlineInputBorder(),
          ),
          child: const SizedBox(
            height: 20,
            child: Center(
              child: SizedBox(
                width: 16,
                height: 16,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            ),
          ),
        ),
      );
    }

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
