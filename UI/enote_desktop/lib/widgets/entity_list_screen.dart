import 'dart:async';

import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

typedef ColumnValueBuilder<T> = dynamic Function(T item);
typedef ColumnCellBuilder<T> = Widget Function(BuildContext context, T item);

class ColumnSpec<T> {
  final String label;
  final ColumnValueBuilder<T> value;
  final TextStyle? Function(T item)? style;
  final ColumnCellBuilder<T>? cellBuilder;

  ColumnSpec({
    required this.label,
    required this.value,
    this.style,
    this.cellBuilder,
  });
}

typedef EntityFetcher<T> = Future<PagedResult<T>> Function(
  int page,
  int pageSize,
  String search,
);

/// How an [EntityListScreen] is presented to the user.
enum EntityListPresentation {
  /// Full page — [Scaffold] with an [AppBar] (the default).
  page,

  /// Embedded headless presentation with no outer [Scaffold]/[AppBar],
  /// forcing inline toolbar layout.
  embedded,
}

/// How rows in [EntityListScreen] are rendered.
enum EntityListStyle {
  /// Standard [DataTable] grid (the default).
  table,

  /// Compact [ListTile]-style rows for reference data / narrow layouts.
  tiles,
}

class EntityListConfig<T> {
  final String? title;
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
  final bool showSearchBar;
  final Widget? filterBar;

  /// When true, render search + filterBar + Add button in a single row
  /// (matching EntityGridScreen's toolbar), with Add as an inline
  /// ElevatedButton.icon instead of a floating action button. Default false
  /// keeps every existing consumer's stacked-rows + FAB layout unchanged.
  final bool inlineToolbar;

  /// Presentation mode of the list screen. When [EntityListPresentation.embedded],
  /// outer Scaffold/AppBar chrome is omitted and [inlineToolbar] is forced.
  final EntityListPresentation presentation;

  /// How data rows are rendered. When [EntityListStyle.tiles], items are
  /// rendered as [ListTile] rows rather than a [DataTable].
  final EntityListStyle listStyle;

  /// Leading icon displayed on each row when [listStyle] is [EntityListStyle.tiles].
  final IconData? rowIcon;

  const EntityListConfig({
    this.title,
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
    this.inlineToolbar = false,
    this.presentation = EntityListPresentation.page,
    this.listStyle = EntityListStyle.table,
    this.rowIcon,
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
        ErrorBanner.show(context, message: userMessage(e));
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
    final useInlineToolbar = widget.config.inlineToolbar ||
        widget.config.presentation == EntityListPresentation.embedded;

    final content = Column(
      children: [
        if (useInlineToolbar)
          _buildInlineToolbar()
        else ...[
          if (widget.config.showSearchBar) _buildSearchBar(),
          if (widget.config.filterBar != null) widget.config.filterBar!,
        ],
        Expanded(
          child: _isLoading
              ? const Center(child: CircularProgressIndicator())
              : _items.isEmpty
              ? const Center(child: Text('Nema podataka.'))
              : (widget.config.listStyle == EntityListStyle.tiles
                  ? _buildTiles()
                  : _buildTable()),
        ),
        _buildPagination(),
      ],
    );

    if (widget.config.presentation == EntityListPresentation.embedded) {
      return content;
    }

    return Scaffold(
      appBar: AppBar(
        title: widget.config.title != null ? Text(widget.config.title!) : null,
        leading: Navigator.of(context).canPop()
            ? IconButton(
                icon: const Icon(Icons.close),
                tooltip: 'Zatvori',
                onPressed: () => Navigator.of(context).pop(),
              )
            : null,
        actions: [
          if (!useInlineToolbar) ...[
            if (widget.config.trailing != null) ...[
              widget.config.trailing!,
              const SizedBox(width: 12),
            ],
            if (widget.config.showAddButton && widget.config.onAdd != null) ...[
              ElevatedButton.icon(
                onPressed: widget.config.onAdd,
                icon: const Icon(Icons.add),
                label: Text(widget.config.addLabel ?? 'Dodaj'),
              ),
              const SizedBox(width: 16),
            ] else if (widget.config.trailing != null)
              const SizedBox(width: 4),
          ],
        ],
      ),
      body: content,
    );
  }

  Widget _buildInlineToolbar() {
    final showSearch = widget.config.showSearchBar;
    final filterBar = widget.config.filterBar;
    final showAdd = widget.config.showAddButton && widget.config.onAdd != null;
    final trailing = widget.config.trailing;
    if (!showSearch && filterBar == null && !showAdd && trailing == null) {
      return const SizedBox.shrink();
    }

    return Padding(
      padding: const EdgeInsets.all(16),
      child: IntrinsicHeight(
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Search + filterBar share one Expanded slot so they're the
            // only thing that gives way when the row is tight (the search
            // field shrinks below its 420 cap instead of overflowing).
            // Keeping them out of the outer Row's flex pool — rather than
            // giving the search field its own Flexible there — means they
            // don't compete with trailing/the add button for space: any
            // width they don't use here stays inside this Expanded instead
            // of leaking past the button as unused trailing space.
            Expanded(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (showSearch)
                    Flexible(
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 420),
                        child: TextField(
                          controller: _searchController,
                          decoration: InputDecoration(
                            hintText: widget.config.searchHint,
                            prefixIcon: const Icon(Icons.search),
                            border: const OutlineInputBorder(),
                          ),
                        ),
                      ),
                    ),
                  if (showSearch && filterBar != null)
                    const SizedBox(width: 12),
                  ?filterBar,
                ],
              ),
            ),
            if (showAdd || trailing != null) const SizedBox(width: 12),
            if (trailing != null) ...[
              trailing,
              const SizedBox(width: 12),
            ],
            if (showAdd)
              ElevatedButton.icon(
                onPressed: widget.config.onAdd,
                icon: const Icon(Icons.add),
                label: Text(widget.config.addLabel ?? 'Dodaj'),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildSearchBar() {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Align(
        alignment: Alignment.centerLeft,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: TextField(
            controller: _searchController,
            decoration: InputDecoration(
              hintText: widget.config.searchHint,
              prefixIcon: const Icon(Icons.search),
              border: const OutlineInputBorder(),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildTiles() {
    final hasActions =
        widget.config.onEdit != null ||
        widget.config.onDelete != null ||
        widget.config.extraActions != null;

    return ListView.separated(
      itemCount: _items.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final item = _items[index];
        final columns = widget.config.columns;
        final title = columns.isNotEmpty
            ? (columns.first.value(item)?.toString() ?? '-')
            : '';

        String? subtitle;
        if (columns.length > 1) {
          subtitle = columns.skip(1).map((col) {
            final val = col.value(item)?.toString() ?? '-';
            return '${col.label}: $val';
          }).join(' · ');
        }

        return ListTile(
          leading: widget.config.rowIcon != null
              ? Icon(widget.config.rowIcon)
              : null,
          title: Text(
            title,
            style: columns.isNotEmpty ? columns.first.style?.call(item) : null,
          ),
          subtitle: subtitle != null ? Text(subtitle) : null,
          onTap: widget.config.onEdit != null
              ? () => widget.config.onEdit!(context, item)
              : null,
          trailing: hasActions
              ? Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (widget.config.onEdit != null)
                      IconButton(
                        icon: const Icon(Icons.edit, size: 18),
                        onPressed: () => widget.config.onEdit!(context, item),
                      ),
                    if (widget.config.onDelete != null)
                      IconButton(
                        icon: const Icon(
                          Icons.delete,
                          size: 18,
                          color: Colors.red,
                        ),
                        onPressed: () => _deleteItem(item),
                      ),
                    if (widget.config.extraActions != null)
                      ...widget.config.extraActions!(context, item),
                  ],
                )
              : null,
        );
      },
    );
  }

  Widget _buildTable() {
    final hasActions =
        widget.config.onEdit != null ||
        widget.config.onDelete != null ||
        widget.config.extraActions != null;

    return SingleChildScrollView(
      child: DataTable(
        columns: [
          ...widget.config.columns.map((c) => DataColumn(label: Text(c.label))),
          if (hasActions) const DataColumn(label: Text('Akcije')),
        ],
        rows: _items.map((item) {
          return DataRow(
            cells: [
              ...widget.config.columns.map(
                (col) => DataCell(
                  col.cellBuilder != null
                      ? col.cellBuilder!(context, item)
                      : Text(
                          col.value(item)?.toString() ?? '-',
                          style: col.style?.call(item),
                        ),
                  onTap: widget.config.onEdit != null
                      ? () => widget.config.onEdit!(context, item)
                      : null,
                ),
              ),
              if (hasActions)
                DataCell(
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (widget.config.onEdit != null)
                        IconButton(
                          icon: const Icon(Icons.edit, size: 18),
                          onPressed: () => widget.config.onEdit!(context, item),
                        ),
                      if (widget.config.onDelete != null)
                        IconButton(
                          icon: const Icon(
                            Icons.delete,
                            size: 18,
                            color: Colors.red,
                          ),
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
      child: Wrap(
        alignment: WrapAlignment.center,
        crossAxisAlignment: WrapCrossAlignment.center,
        spacing: 16,
        runSpacing: 8,
        children: [
          Text('Stranica $_currentPage od $totalPages'),
          Text('Ukupno: $_totalCount'),
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
