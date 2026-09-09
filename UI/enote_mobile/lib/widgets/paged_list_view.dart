import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import 'async_state_view.dart';

class PagedListView<T> extends StatefulWidget {
  final PagedFetchController<T> controller;
  final Widget Function(BuildContext context, T item) itemBuilder;
  final bool showSearch;
  final String searchHint;
  final Widget? filterRow;
  final String emptyMessage;
  final Widget? emptyAction;
  final Object? error;
  final VoidCallback? onRetry;

  const PagedListView({
    super.key,
    required this.controller,
    required this.itemBuilder,
    this.showSearch = true,
    this.searchHint = 'Pretraži…',
    this.filterRow,
    this.emptyMessage = 'Nema rezultata za pretragu.',
    this.emptyAction,
    this.error,
    this.onRetry,
  });

  @override
  State<PagedListView<T>> createState() => _PagedListViewState<T>();
}

class _PagedListViewState<T> extends State<PagedListView<T>> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onController);
    widget.controller.load();
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onController);
    super.dispose();
  }

  void _onController() {
    if (mounted) {
      setState(() {});
    }
  }

  @override
  Widget build(BuildContext context) {
    final items = widget.controller.items;
    final isLoading = widget.controller.isLoading && items.isEmpty;
    return Column(
      children: [
        if (widget.showSearch)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: TextField(
              controller: widget.controller.searchController,
              decoration: InputDecoration(
                hintText: widget.searchHint,
                prefixIcon: const Icon(Icons.search_outlined),
              ),
            ),
          ),
        if (widget.filterRow != null) widget.filterRow!,
        Expanded(
          child: RefreshIndicator(
            onRefresh: () => widget.controller.load(),
            // The state view replaces the list only when there is no list to
            // show. A failed reload on top of loaded rows keeps the rows:
            // taking the state-view branch with items present used to render
            // an empty `SizedBox`, blanking the screen.
            child: isLoading || items.isEmpty
                ? ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: [
                      AsyncStateView(
                        isLoading: isLoading,
                        error: widget.error,
                        isEmpty: items.isEmpty,
                        emptyMessage: widget.emptyMessage,
                        emptyAction: widget.emptyAction,
                        onRetry:
                            widget.onRetry ?? () => widget.controller.load(),
                        child: const SizedBox.shrink(),
                      ),
                    ],
                  )
                : ListView.builder(
                    itemCount: items.length,
                    itemBuilder: (context, index) =>
                        widget.itemBuilder(context, items[index]),
                  ),
          ),
        ),
        PagedPaginationBar(controller: widget.controller),
      ],
    );
  }
}
