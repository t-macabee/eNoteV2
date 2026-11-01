import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';
import 'entity_empty_state.dart';
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
        message:
            widget.config.deleteConfirmMessageOf?.call(item) ??
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
    final isEmbedded =
        widget.config.presentation == EntityListPresentation.embedded;

    final listBody = _controller.isLoading
        ? const Center(child: CircularProgressIndicator())
        : _controller.items.isEmpty
        ? EntityEmptyState(
            searching: _controller.searchController.text.trim().isNotEmpty,
          )
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
        EntityToolbar(
          searchController: _controller.searchController,
          showSearch: widget.config.showSearchBar,
          searchHint: widget.config.searchHint,
          filterBar: widget.config.filterBar,
          trailing: widget.config.trailing,
          showAdd: widget.config.showAddButton && widget.config.onAdd != null,
          onAdd: widget.config.onAdd,
          addLabel: widget.config.addLabel,
        ),
        if (isEmbedded)
          Flexible(child: listBody)
        else
          Expanded(child: listBody),
        Container(
          decoration: const BoxDecoration(
            border: Border(top: BorderSide(color: AppTheme.outline, width: 1)),
          ),
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          child: PagedPaginationBar(controller: _controller),
        ),
      ],
    );

    if (isEmbedded) return content;

    final canClose = Navigator.of(context).canPop();
    return Scaffold(
      appBar: canClose
          ? AppBar(
              title: widget.config.title != null
                  ? Text(widget.config.title!)
                  : null,
              leading: IconButton(
                icon: const Icon(Icons.close),
                tooltip: 'Zatvori',
                onPressed: () => Navigator.of(context).pop(),
              ),
            )
          : null,
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: AppSize.contentMaxWidth),
          child: content,
        ),
      ),
    );
  }
}
