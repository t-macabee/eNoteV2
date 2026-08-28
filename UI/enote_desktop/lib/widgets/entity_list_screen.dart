import 'dart:async';

import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

typedef ColumnValueBuilder<T> = dynamic Function(T item);

class ColumnSpec<T> {
  final String label;
  final ColumnValueBuilder<T> value;
  final TextStyle? Function(T item)? style;

  ColumnSpec({
    required this.label,
    required this.value,
    this.style,
  });
}

typedef EntityFetcher<T> = Future<PagedResult<T>> Function(
    int page, int pageSize, String search);

class EntityListConfig<T> {
  final String title;
  final List<ColumnSpec<T>> columns;
  final String searchHint;
  final EntityFetcher<T> fetcher;
  final void Function(BuildContext context, T item)? onEdit;
  final Future<bool?> Function(BuildContext context, T item)? onDelete;
  final List<Widget> Function(BuildContext context, T item)? extraActions;
  final VoidCallback? onAdd;
  final Widget? trailing;
  final String? addLabel;
  final bool showAddButton;
  final bool showDeleteConfirmation;

  /// Shows the built-in free-text search box. Set false when the backend
  /// endpoint doesn't support free-text search (the box would otherwise sit
  /// there doing nothing) — pair with [filterBar] for real filter controls.
  final bool showSearchBar;

  /// Optional filter controls rendered above the table, in place of (or
  /// alongside) the search box — e.g. status/FK dropdowns for a resource the
  /// backend only filters by discrete fields, not free text.
  final Widget? filterBar;

  const EntityListConfig({
    required this.title,
    required this.columns,
    required this.fetcher,
    this.searchHint = 'Pretraži...',
    this.onEdit,
    this.onDelete,
    this.extraActions,
    this.onAdd,
    this.trailing,
    this.addLabel = 'Dodaj',
    this.showAddButton = true,
    this.showDeleteConfirmation = true,
    this.showSearchBar = true,
    this.filterBar,
  });
}

class EntityListScreen<T> extends StatefulWidget {
  final EntityListConfig<T> config;

  const EntityListScreen({super.key, required this.config});

  @override
  State<EntityListScreen<T>> createState() => EntityListScreenState<T>();
}

class EntityListScreenState<T> extends State<EntityListScreen<T>> {
  late final TextEditingController _searchController;
  List<T> _items = [];
  int _currentPage = 1;
  final int _pageSize = 20;
  int? _totalCount;
  bool _isLoading = false;
  String _currentSearch = '';
  Timer? _searchDebounce;
  int _requestId = 0;

  @override
  void initState() {
    super.initState();
    _searchController = TextEditingController();
    _searchController.addListener(_onSearchChanged);
    _loadPage();
  }

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _searchController.removeListener(_onSearchChanged);
    _searchController.dispose();
    super.dispose();
  }

  void _onSearchChanged() {
    final value = _searchController.text;
    _searchDebounce?.cancel();
    _searchDebounce = Timer(const Duration(milliseconds: 300), () {
      if (value != _currentSearch) {
        _currentSearch = value;
        _currentPage = 1;
        _loadPage();
      }
    });
  }

  Future<void> _loadPage() async {
    final requestId = ++_requestId;
    setState(() => _isLoading = true);

    try {
      final result = await widget.config.fetcher(
        _currentPage,
        _pageSize,
        _currentSearch,
      );
      if (requestId != _requestId) return;
      setState(() {
        _items = result.items;
        _totalCount = result.totalCount;
      });
    } catch (e) {
      if (requestId != _requestId) return;
      if (mounted) {
        ErrorBanner.show(
          context,
          message: userMessage(e),
        );
      }
    } finally {
      if (requestId == _requestId && mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  void refresh() {
    _loadPage();
  }

  Future<void> _deleteItem(T item) async {
    if (widget.config.showDeleteConfirmation) {
      final confirmed = await confirmDialog(
        context: context,
        title: 'Potvrdite brisanje',
        message: 'Da li ste sigurni da želite da obrišete ovaj zapis?',
      );
      if (confirmed != true) return;
    }

    if (!mounted) return;
    await widget.config.onDelete?.call(context, item);
    _loadPage();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.config.title),
        actions: [
          if (widget.config.trailing != null) widget.config.trailing!,
        ],
      ),
      body: Column(
        children: [
          if (widget.config.showSearchBar) _buildSearchBar(),
          if (widget.config.filterBar != null) widget.config.filterBar!,
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _items.isEmpty
                    ? const Center(child: Text('Nema podataka.'))
                    : _buildTable(),
          ),
          _buildPagination(),
        ],
      ),
      floatingActionButton: widget.config.showAddButton
          ? FloatingActionButton.extended(
              onPressed: widget.config.onAdd,
              label: Text(widget.config.addLabel ?? 'Dodaj'),
            )
          : null,
    );
  }

  Widget _buildSearchBar() {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: TextField(
        controller: _searchController,
        decoration: InputDecoration(
          hintText: widget.config.searchHint,
          prefixIcon: const Icon(Icons.search),
          border: const OutlineInputBorder(),
        ),
      ),
    );
  }

  Widget _buildTable() {
    final hasActions = widget.config.onEdit != null || widget.config.onDelete != null || widget.config.extraActions != null;

    return SingleChildScrollView(
      child: DataTable(
        columns: [
          ...widget.config.columns.map((c) => DataColumn(label: Text(c.label))),
          if (hasActions) const DataColumn(label: Text('Akcije')),
        ],
        rows: _items.map((item) {
          return DataRow(
            cells: [
              ...widget.config.columns.map((col) => DataCell(
                    Text(
                      col.value(item)?.toString() ?? '-',
                      style: col.style?.call(item),
                    ),
                    onTap: widget.config.onEdit != null
                        ? () => widget.config.onEdit!(context, item)
                        : null,
                  )),
              if (hasActions)
                DataCell(
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (widget.config.onEdit != null)
                        IconButton(
                          icon: const Icon(Icons.edit, size: 18),
                          onPressed: () =>
                              widget.config.onEdit!(context, item),
                        ),
                      if (widget.config.onDelete != null)
                        IconButton(
                          icon: const Icon(Icons.delete,
                              size: 18, color: Colors.red),
                          onPressed: () => _deleteItem(item),
                        ),
                      if (widget.config.extraActions != null)
                        ...widget.config.extraActions!(context, item),
                    ],
                  ),
                ),
            ],
          );
        }).toList(),
      ),
    );
  }

  Widget _buildPagination() {
    if (_totalCount == null) return const SizedBox.shrink();

    final totalPages = (_totalCount! / _pageSize).ceil();
    final hasPrev = _currentPage > 1;
    final hasNext = _currentPage < totalPages;

    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Text('Stranica $_currentPage od $totalPages'),
          const SizedBox(width: 16),
          Text('Ukupno: $_totalCount'),
          const SizedBox(width: 16),
          TextButton.icon(
            onPressed: hasPrev
                ? () {
                    setState(() => _currentPage--);
                    _loadPage();
                  }
                : null,
            icon: const Icon(Icons.chevron_left),
            label: const Text('Prethodna'),
          ),
          TextButton.icon(
            onPressed: hasNext
                ? () {
                    setState(() => _currentPage++);
                    _loadPage();
                  }
                : null,
            icon: const Icon(Icons.chevron_right),
            label: const Text('Sledeća'),
          ),
        ],
      ),
    );
  }
}
