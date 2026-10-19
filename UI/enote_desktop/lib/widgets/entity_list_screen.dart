import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

import 'entity_list_sections.dart';

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

enum EntityListPresentation {
  page,

  embedded,
}

enum EntityListStyle {
  table,

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
  final String Function(T item)? deleteConfirmMessageOf;
  final String? deletedMessage;
  final bool showSearchBar;
  final Widget? filterBar;

  final EntityListPresentation presentation;

  final EntityListStyle listStyle;

  final IconData? rowIcon;

  final List<int>? tileSubtitleColumns;

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
    this.deleteConfirmMessageOf,
    this.deletedMessage,
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
  static const int _pageSize = 20;

  late final PagedFetchController<T> _controller;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<T>(
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

  void refresh({bool resetPage = false}) =>
      _controller.refresh(resetPage: resetPage);

  Future<void> _deleteItem(T item) async {
    if (widget.config.showDeleteConfirmation) {
      final confirmed = await confirmDialog(
        context: context,
        title: 'Potvrdite brisanje',
        message: widget.config.deleteConfirmMessageOf?.call(item) ??
            'Da li ste sigurni da želite da obrišete ovaj zapis?',
      );
      if (confirmed != true) return;
    }

    if (!mounted) return;
    try {
      final deleted = await widget.config.onDelete?.call(context, item);
      final deletedMessage = widget.config.deletedMessage;
      if (mounted && deleted == true && deletedMessage != null) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(deletedMessage)));
      }
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    }
    if (mounted) {
      _controller.load();
    }
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
              ? EntityListTiles<T>(
                  config: widget.config,
                  items: _controller.items,
                  onDelete: _deleteItem,
                )
              : EntityListTable<T>(
                  config: widget.config,
                  items: _controller.items,
                  onDelete: _deleteItem,
                ));

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
          if (widget.config.showSearchBar)
            EntityListSearchBar(
              controller: _controller.searchController,
              hint: widget.config.searchHint,
            ),
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

  Widget _buildPagination() {
    return PagedPaginationBar(controller: _controller);
  }
}

