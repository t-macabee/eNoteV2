import 'package:flutter/material.dart';

class AsyncDropdown<T> extends StatefulWidget {
  final String label;
  final Future<List<T>> Function() fetcher;
  final String Function(T item) itemLabel;
  final Object Function(T item) itemId;
  final Object? value;
  final void Function(Object? id, T? item) onChanged;
  final String? Function(Object? value)? validator;
  final bool enabled;
  final String hint;
  final String? emptyOptionLabel;
  final double? width;

  const AsyncDropdown({
    super.key,
    required this.label,
    required this.fetcher,
    required this.itemLabel,
    required this.itemId,
    required this.onChanged,
    this.value,
    this.validator,
    this.enabled = true,
    this.hint = 'Odaberite...',
    this.emptyOptionLabel,
    this.width,
  });

  @override
  State<AsyncDropdown<T>> createState() => _AsyncDropdownState<T>();
}

class _AsyncDropdownState<T> extends State<AsyncDropdown<T>> {
  late Future<List<T>> _future;
  Object? _lastValue;
  int _fieldGeneration = 0;

  @override
  void initState() {
    super.initState();
    _future = widget.fetcher();
    _lastValue = widget.value;
  }

  @override
  void didUpdateWidget(covariant AsyncDropdown<T> oldWidget) {
    super.didUpdateWidget(oldWidget);

    if (widget.value != _lastValue) {
      _lastValue = widget.value;
      setState(() {
        _fieldGeneration++;
      });
    }
  }

  Future<void> _reload() async {
    setState(() {
      _future = widget.fetcher();
    });
    await _future;
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: widget.width,
      child: _buildField(context),
    );
  }

  Widget _buildField(BuildContext context) {
    return FutureBuilder<List<T>>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return InputDecorator(
            decoration: InputDecoration(
              labelText: widget.label,
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
          );
        }

        if (snapshot.hasError) {
          return InputDecorator(
            decoration: InputDecoration(
              labelText: widget.label,
              border: const OutlineInputBorder(),
              errorText: 'Neuspjelo učitavanje.',
              suffixIcon: IconButton(
                icon: const Icon(Icons.refresh),
                tooltip: 'Pokušaj ponovo',
                onPressed: _reload,
              ),
            ),
            child: const Text('-'),
          );
        }

        final items = snapshot.data ?? const [];
        final isEmpty = items.isEmpty && widget.emptyOptionLabel == null;
        final currentValue = items.any((e) => widget.itemId(e) == widget.value)
            ? widget.value
            : null;

        return DropdownButtonFormField<Object>(
          key: ValueKey(_fieldGeneration),
          initialValue: currentValue,
          decoration: InputDecoration(
            labelText: widget.label,
            border: const OutlineInputBorder(),
            helperText: isEmpty ? 'Nema dostupnih opcija' : null,
            suffixIcon: isEmpty
                ? IconButton(
                    icon: const Icon(Icons.refresh),
                    tooltip: 'Pokušaj ponovo',
                    onPressed: _reload,
                  )
                : null,
          ),
          hint: Text(widget.emptyOptionLabel ?? widget.hint),
          isExpanded: true,
          items: [
            if (widget.emptyOptionLabel != null)
              DropdownMenuItem<Object>(
                value: null,
                child: Text(widget.emptyOptionLabel!),
              ),
            ...items.map(
              (e) => DropdownMenuItem<Object>(
                value: widget.itemId(e),
                child: Text(widget.itemLabel(e)),
              ),
            ),
          ],
          onChanged: widget.enabled && !isEmpty
              ? (id) {
                  final item = items.cast<T?>().firstWhere(
                    (e) => e != null && widget.itemId(e) == id,
                    orElse: () => null,
                  );
                  widget.onChanged(id, item);
                }
              : null,
          validator: widget.validator,
        );
      },
    );
  }
}
