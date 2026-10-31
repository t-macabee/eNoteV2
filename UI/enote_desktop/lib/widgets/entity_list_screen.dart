import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

import 'entity_list_config.dart';
import 'entity_list_sections.dart';

export 'entity_list_config.dart';

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

