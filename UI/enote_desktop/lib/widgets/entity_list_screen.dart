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

  /// Presentation mode of the list screen. When [EntityListPresentation.embedded],
  /// outer Scaffold/AppBar chrome is omitted and the inline toolbar is forced.
  final EntityListPresentation presentation;

  /// How data rows are rendered. When [EntityListStyle.tiles], items are
  /// rendered as [ListTile] rows rather than a [DataTable].
  final EntityListStyle listStyle;

  /// Leading icon displayed on each row when [listStyle] is [EntityListStyle.tiles].
  final IconData? rowIcon;

  /// Indices into [columns] rendered in the tile subtitle when [listStyle]
  /// is [EntityListStyle.tiles]. When `null` (the default), all columns
  /// after the first are shown, preserving the historical behavior.
  final List<int>? tileSubtitleColumns;

  /// Tap handler for the tile row when [listStyle] is [EntityListStyle.tiles].
  /// When non-null it is used for [ListTile.onTap]; otherwise [onEdit] is
  /// used as before.
  final void Function(BuildContext context, T item)? onRowTap;

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
    this.presentation = EntityListPresentation.page,
    this.listStyle = EntityListStyle.table,
    this.rowIcon,
    this.tileSubtitleColumns,
    this.onRowTap,
  });
}

class EntityListScreen<T> extends StatefulWidget {
  final EntityListConfig<T> config;

  const EntityListScreen({super.key, required this.config});

  @override
  State<EntityListScreen<T>> createState() => EntityListScreenState<T>();
}

class EntityListScreenState<T> extends State<EntityListScreen<T>> {
  /// Fixed at 20 for every list screen. The grid's page size is
  /// config-driven; this one deliberately is not.
  static const int _pageSize = 20;

  late final PagedFetchController<T> _controller;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<T>(
      // Delegate rather than passing `widget.config.fetcher` directly: screens
      // rebuild their config on every build, and some fetcher closures read
      // filter state as fields at call time. Going through `widget.config`
      // here keeps that behaviour.
      fetcher: (page, pageSize, search) =>
          widget.config.fetcher(page, pageSize, search),
      pageSize: _pageSize,
      onError: (e) {
        if (mounted) {
          ErrorBanner.show(context, message: userMessage(e));
        }
      },
    );
    _controller.addListener(_onControllerChanged);
    _controller.load();
  }

  @override
  void dispose() {
    _controller.removeListener(_onControllerChanged);
    _controller.dispose();
    super.dispose();
  }

  void _onControllerChanged() {
    if (mounted) setState(() {});
  }

  /// Re-runs the current page/search — call after add/edit/delete, or when
  /// external filters (a [filterBar] control) change and should re-trigger
  /// the fetch from page 1.
  ///
  /// [resetPage] defaults to false so the existing no-arg callers keep their
  /// current behaviour.
  void refresh({bool resetPage = false}) =>
      _controller.refresh(resetPage: resetPage);

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
    _controller.load();
  }

  @override
  Widget build(BuildContext context) {
    final useInlineToolbar = widget.config.presentation == EntityListPresentation.embedded;
    final isEmbedded =
        widget.config.presentation == EntityListPresentation.embedded;

    final listBody = _controller.isLoading
        ? const Center(child: CircularProgressIndicator())
        : _controller.items.isEmpty
        ? const Center(child: Text('Nema podataka.'))
        : (widget.config.listStyle == EntityListStyle.tiles
              ? _buildTiles()
              : _buildTable());

    final content = Column(
      mainAxisSize: isEmbedded ? MainAxisSize.min : MainAxisSize.max,
      children: [
        if (useInlineToolbar)
          EntityToolbar(
            searchController: _controller.searchController,
            showSearch: widget.config.showSearchBar,
            searchHint: widget.config.searchHint,
            filterBar: widget.config.filterBar,
            trailing: widget.config.trailing,
            showAdd:
                widget.config.showAddButton && widget.config.onAdd != null,
            onAdd: widget.config.onAdd,
            addLabel: widget.config.addLabel,
          )
        else ...[
          if (widget.config.showSearchBar) _buildSearchBar(),
          if (widget.config.filterBar != null) widget.config.filterBar!,
        ],
        if (isEmbedded) Flexible(child: listBody) else Expanded(child: listBody),
        const SizedBox(height: 12),
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

  /// The stacked (non-inline) search bar. List-only: the grid has no
  /// counterpart, and this is the layout the default list screens use, with
  /// their Add button in the AppBar actions above.
  Widget _buildSearchBar() {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Align(
        alignment: Alignment.centerLeft,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: TextField(
            controller: _controller.searchController,
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

    final items = _controller.items;
    final isEmbedded =
        widget.config.presentation == EntityListPresentation.embedded;

    return ListView.separated(
      shrinkWrap: isEmbedded,
      itemCount: items.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final item = items[index];
        final columns = widget.config.columns;
        final title = columns.isNotEmpty
            ? (columns.first.value(item)?.toString() ?? '-')
            : '';

        String? subtitle;
        Widget? subtitleWidget;
        final subtitleIndices = widget.config.tileSubtitleColumns;
        List<ColumnSpec<T>> subtitleCols;
        if (subtitleIndices != null) {
          subtitleCols = <ColumnSpec<T>>[];
          for (final i in subtitleIndices) {
            if (i < 0 || i >= columns.length) continue;
            subtitleCols.add(columns[i]);
          }
        } else if (columns.length > 1) {
          subtitleCols = columns.skip(1).toList();
        } else {
          subtitleCols = <ColumnSpec<T>>[];
        }
        if (subtitleCols.isNotEmpty) {
          final hasCustom =
              subtitleCols.any((col) => col.cellBuilder != null);
          if (!hasCustom) {
            final parts = <String>[];
            for (final col in subtitleCols) {
              final val = col.value(item)?.toString() ?? '-';
              parts.add('${col.label}: $val');
            }
            if (parts.isNotEmpty) subtitle = parts.join(' · ');
          } else {
            subtitleWidget = Wrap(
              spacing: 8,
              runSpacing: 4,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                for (final col in subtitleCols)
                  if (col.cellBuilder != null)
                    col.cellBuilder!(context, item)
                  else
                    Text('${col.label}: ${col.value(item)?.toString() ?? '-'}'),
              ],
            );
          }
        }

        final onRowTap = widget.config.onRowTap ?? widget.config.onEdit;
        final extraWidgets =
            widget.config.extraActions?.call(context, item) ??
            const <Widget>[];
        final hasEditDelete =
            widget.config.onEdit != null || widget.config.onDelete != null;
        return ListTile(
          leading: widget.config.rowIcon != null
              ? Icon(widget.config.rowIcon)
              : null,
          title: Text(
            title,
            style: columns.isNotEmpty ? columns.first.style?.call(item) : null,
          ),
          subtitle: subtitleWidget ?? (subtitle != null ? Text(subtitle) : null),
          onTap: onRowTap != null
              ? () => onRowTap(context, item)
              : null,
          trailing: hasActions
              ? Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    ...extraWidgets,
                    if (extraWidgets.isNotEmpty && hasEditDelete)
                      const SizedBox(
                        height: 20,
                        child: VerticalDivider(width: 12),
                      ),
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
        rows: _controller.items.map((item) {
          final extraWidgets =
              widget.config.extraActions?.call(context, item) ??
              const <Widget>[];
          final hasEditDelete =
              widget.config.onEdit != null || widget.config.onDelete != null;
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
                      ...extraWidgets,
                      if (extraWidgets.isNotEmpty && hasEditDelete)
                        const SizedBox(
                          height: 20,
                          child: VerticalDivider(width: 12),
                        ),
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
    return PagedPaginationBar(controller: _controller);
  }
}
