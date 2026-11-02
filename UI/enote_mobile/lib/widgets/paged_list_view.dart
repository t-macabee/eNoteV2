import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import 'async_state_view.dart';
import 'load_more_footer.dart';

class PagedListView<T> extends StatefulWidget {
  final PagedFetchController<T> controller;
  final Widget Function(BuildContext context, T item) itemBuilder;
  final bool showSearch;
  final String searchHint;
  final Widget? statusTabs;
  final Widget? filterRow;
  final String emptyMessage;
  final Widget? emptyAction;
  final Future<void> Function()? onRefresh;

  const PagedListView({
    super.key,
    required this.controller,
    required this.itemBuilder,
    this.showSearch = true,
    this.searchHint = 'Pretraži…',
    this.statusTabs,
    this.filterRow,
    this.emptyMessage = 'Nema podataka.',
    this.emptyAction,
    this.onRefresh,
  });

  @override
  State<PagedListView<T>> createState() => _PagedListViewState<T>();
}

class _PagedListViewState<T> extends State<PagedListView<T>> {
  static const _headerPadding = EdgeInsets.fromLTRB(
    AppSpacing.lg,
    AppSpacing.md,
    AppSpacing.lg,
    AppSpacing.sm,
  );

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

  Future<void> _refresh() async {
    await Future.wait([
      widget.controller.refresh(resetPage: true),
      if (widget.onRefresh != null) widget.onRefresh!(),
    ]);
  }

  String get _emptyMessage => widget.controller.search.isEmpty
      ? widget.emptyMessage
      : 'Nema rezultata za pretragu.';

  Widget _header() {
    final parts = [
      if (widget.showSearch)
        TextField(
          controller: widget.controller.searchController,
          decoration: InputDecoration(
            hintText: widget.searchHint,
            prefixIcon: const Icon(Icons.search_outlined),
          ),
        ),
      if (widget.statusTabs != null) widget.statusTabs!,
      if (widget.filterRow != null) widget.filterRow!,
    ];
    if (parts.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: _headerPadding,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          for (var i = 0; i < parts.length; i++) ...[
            if (i > 0) const SizedBox(height: AppSpacing.sm),
            parts[i],
          ],
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final controller = widget.controller;
    final items = controller.items;
    return Column(
      children: [
        _header(),
        Expanded(
          child: RefreshIndicator(
            onRefresh: _refresh,
            child: items.isEmpty
                ? ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: [
                      AsyncStateView(
                        isLoading: controller.isLoading,
                        error: controller.error,
                        isEmpty: true,
                        emptyMessage: _emptyMessage,
                        emptyAction: widget.emptyAction,
                        onRetry: () => controller.load(),
                        child: const SizedBox.shrink(),
                      ),
                    ],
                  )
                : ListView.separated(
                    itemCount: items.length + 1,
                    separatorBuilder: (_, index) => index < items.length - 1
                        ? const Divider(height: 1)
                        : const SizedBox.shrink(),
                    itemBuilder: (context, index) {
                      if (index == items.length) {
                        return LoadMoreFooter(
                          hasMore: controller.hasNextPage,
                          isLoading: controller.isLoadingMore,
                          error: controller.loadMoreError,
                          onLoadMore: () => controller.loadMore(),
                        );
                      }
                      return widget.itemBuilder(context, items[index]);
                    },
                  ),
          ),
        ),
      ],
    );
  }
}
