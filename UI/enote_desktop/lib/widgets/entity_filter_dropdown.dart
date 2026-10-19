import 'package:flutter/material.dart';

/// Shared controlled filter dropdown for entity list/grid filter bars.
///
/// Takes [value:] (controlled), not `initialValue:` — the previous
/// `initialValue:` usage is why a programmatic filter reset did not
/// update the dropdown (see the value-sync workaround in
/// `async_dropdown.dart:43-53`).
///
/// Does NOT own refresh: each caller keeps calling its own `_applyFilters`
/// (or setState + refresh) in [onChanged]. The `setState`-then-`refresh`
/// timing is load-bearing (see the doc comment on `_fetchUsers` in
/// `user_grid_screen.dart`); moving `refresh(resetPage: true)` in here would
/// break it.
///
/// A caller whose options load from the API passes [isLoading] while the
/// request is in flight, and [errorText] plus [onRetry] when it fails.
/// Dropdowns that fetch their own options stay on [AsyncDropdown].
class EntityFilterDropdown<T> extends StatelessWidget {
  final String label;
  final List<DropdownMenuItem<T>> items;
  final T? value;
  final ValueChanged<T?>? onChanged;
  final double width;
  final bool isExpanded;
  final bool outlined;

  /// Renders a spinner in place of the control while the filter's options are
  /// loading.
  final bool isLoading;

  /// When set, renders this message with a retry action instead of a filter
  /// that would silently offer only its "all" entry.
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

    // Controlled via [value]: Flutter 3.33+ deprecated
    // DropdownButtonFormField.value in favour of initialValue, so we drive
    // updates with key: ValueKey(value) + initialValue (same trick as
    // AsyncDropdown's _fieldGeneration). Changing the key forces a rebuild
    // with the new initialValue, so programmatic resets update the UI —
    // unlike the old bare initialValue: without a key.
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
