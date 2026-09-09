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
/// timing is load-bearing (see comments in `user_grid_screen.dart:192-199`);
/// moving `refresh(resetPage: true)` in here would break it.
///
/// Do NOT use for genuinely-async filters (e.g. the instructor filter in
/// `event_list_screen.dart`, or the instrument filter in
/// `rental_list_screen.dart` which uses a FutureBuilder) — those stay on
/// [AsyncDropdown] / FutureBuilder.
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
