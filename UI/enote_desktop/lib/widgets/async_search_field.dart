import 'dart:async';

import 'package:enote_core/enote_core.dart';
import 'package:flutter/material.dart';

class AsyncSearchField<T extends Object> extends StatefulWidget {
  static const int pageSize = 20;
  static const Duration debounce = Duration(milliseconds: 300);

  final String label;
  final Future<PagedResult<T>> Function(Map<String, dynamic> query) search;
  final String searchField;
  final Map<String, dynamic>? filters;
  final String Function(T item) itemLabel;
  final Object Function(T item) itemId;
  final Object? value;
  final void Function(Object? id, T? item) onChanged;
  final String? Function(Object? value)? validator;
  final bool enabled;
  final String hint;
  final String? clearTooltip;
  final double? width;

  const AsyncSearchField({
    super.key,
    required this.label,
    required this.search,
    required this.searchField,
    required this.itemLabel,
    required this.itemId,
    required this.onChanged,
    this.filters,
    this.value,
    this.validator,
    this.enabled = true,
    this.hint = 'Pretražite...',
    this.clearTooltip = 'Očisti odabir',
    this.width,
  });

  @override
  State<AsyncSearchField<T>> createState() => _AsyncSearchFieldState<T>();
}

class _AsyncSearchFieldState<T extends Object>
    extends State<AsyncSearchField<T>> {
  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  Future<Iterable<T>> _options(String text) async {
    final term = text.trim();
    if (term.isEmpty) return const [];

    _debounce?.cancel();
    final completer = Completer<void>();
    _debounce = Timer(AsyncSearchField.debounce, completer.complete);
    await completer.future;

    if (!mounted) return const [];

    try {
      final page = await widget.search(
        pagedQuery(
          1,
          AsyncSearchField.pageSize,
          term,
          searchField: widget.searchField,
          filters: widget.filters,
        ),
      );
      return page.items;
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
      return const [];
    }
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: widget.width,
      child: Autocomplete<T>(
        optionsBuilder: (textEditingValue) => _options(textEditingValue.text),
        displayStringForOption: widget.itemLabel,
        onSelected: (item) => widget.onChanged(widget.itemId(item), item),
        fieldViewBuilder:
            (context, controller, focusNode, onFieldSubmitted) {
              return TextFormField(
                controller: controller,
                focusNode: focusNode,
                enabled: widget.enabled,
                decoration: InputDecoration(
                  labelText: widget.label,
                  hintText: widget.hint,
                  border: const OutlineInputBorder(),
                  errorMaxLines: 3,
                  suffixIcon: widget.value == null
                      ? const Icon(Icons.search)
                      : IconButton(
                          icon: const Icon(Icons.clear),
                          tooltip: widget.clearTooltip,
                          onPressed: () {
                            controller.clear();
                            widget.onChanged(null, null);
                          },
                        ),
                ),
                validator: (_) => widget.validator?.call(widget.value),
              );
            },
        optionsViewBuilder: (context, onSelected, options) {
          return Align(
            alignment: Alignment.topLeft,
            child: Material(
              elevation: 4,
              borderRadius: BorderRadius.circular(AppRadius.md),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxHeight: 280),
                child: ListView(
                  padding: EdgeInsets.zero,
                  shrinkWrap: true,
                  children: [
                    for (final option in options)
                      ListTile(
                        dense: true,
                        title: Text(widget.itemLabel(option)),
                        onTap: () => onSelected(option),
                      ),
                  ],
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}
