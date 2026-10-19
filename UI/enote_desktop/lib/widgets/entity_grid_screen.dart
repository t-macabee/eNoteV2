import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../theme/app_theme.dart';

typedef EntityGridFetcher<T> = Future<PagedResult<T>> Function(
  int page,
  int pageSize,
  String search,
);

class EntityGridConfig<T> {
  final String? title;
  final EntityGridFetcher<T> fetcher;
  final String Function(T item) titleOf;
  final String? Function(T item)? subtitleOf;
  final String? Function(T item)? imageUrlOf;
  final IconData placeholderIcon;
  final String? Function(T item)? badgeOf;
  final void Function(BuildContext context, T item)? onTap;

  final List<Widget> Function(BuildContext context, T item)? cardActions;
  final VoidCallback? onAdd;
  final String? addLabel;
  final bool showAddButton;
  final String searchHint;
  final bool showSearchBar;

  final bool embedded;

  final Widget? trailing;

  final Widget? filterBar;


  final double maxCrossAxisExtent;
  final double childAspectRatio;
  final int pageSize;
  final String emptyMessage;

  final String Function(T item)? groupKeyOf;

  const EntityGridConfig({
    this.title,
    required this.fetcher,
    required this.titleOf,
    this.subtitleOf,
    this.imageUrlOf,
    this.placeholderIcon = Icons.image_outlined,
    this.badgeOf,
    this.onTap,
    this.cardActions,
    this.onAdd,
    this.addLabel = 'Dodaj',
    this.showAddButton = true,
    this.searchHint = 'Pretraži...',
    this.showSearchBar = true,
    this.embedded = false,
    this.trailing,
    this.filterBar,
    this.maxCrossAxisExtent = 220,
    this.childAspectRatio = 0.85,
    this.pageSize = 24,
    this.emptyMessage = 'Nema podataka.',
    this.groupKeyOf,
  });
}

class EntityGridScreen<T> extends StatefulWidget {
  final EntityGridConfig<T> config;

  const EntityGridScreen({super.key, required this.config});

  @override
  State<EntityGridScreen<T>> createState() => EntityGridScreenState<T>();
}

class EntityGridScreenState<T> extends State<EntityGridScreen<T>> {
  late final PagedFetchController<T> _controller;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<T>(
      fetcher: (page, pageSize, search) =>
          widget.config.fetcher(page, pageSize, search),
      pageSize: widget.config.pageSize,
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

  @override
  Widget build(BuildContext context) {
    final content = _buildContent();
    if (widget.config.embedded) {
      return content;
    }
    return Scaffold(
      appBar: widget.config.title != null
          ? AppBar(title: Text(widget.config.title!))
          : null,
      body: content,
    );
  }

  Widget _buildContent() {
    return Column(
      children: [
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
        ),
        Expanded(
          child: _controller.isLoading
              ? const Center(child: CircularProgressIndicator())
              : _controller.items.isEmpty
                  ? Center(
                      child: Text(
                        widget.config.emptyMessage,
                        style: const TextStyle(color: AppTheme.textSecondary),
                      ),
                    )
                  : _buildBody(),
        ),
        _buildPagination(),
      ],
    );
  }

  Widget _buildGridView(List<T> items) {
    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: SliverGridDelegateWithMaxCrossAxisExtent(
        maxCrossAxisExtent: widget.config.maxCrossAxisExtent,
        childAspectRatio: widget.config.childAspectRatio,
        mainAxisSpacing: 24,
        crossAxisSpacing: 24,
      ),
      itemCount: items.length,
      itemBuilder: (context, index) {
        final item = items[index];
        return _EntityGridCard<T>(
          item: item,
          config: widget.config,
        );
      },
    );
  }

  List<Widget> _buildGroupedChildren(String Function(T item) groupKeyOf) {
    final groups = <String, List<T>>{};
    for (final item in _controller.items) {
      groups.putIfAbsent(groupKeyOf(item), () => []).add(item);
    }

    final sortedKeys =
        groups.keys.toList()..sort((a, b) => a.toLowerCase().compareTo(b.toLowerCase()));

    final children = <Widget>[];
    for (var i = 0; i < sortedKeys.length; i++) {
      final key = sortedKeys[i];
      children.addAll([
        EntitySectionLabel(key),
        const SizedBox(height: 12),
        _buildGridView(groups[key]!),
      ]);
      if (i != sortedKeys.length - 1) {
        children.add(const SizedBox(height: 24));
      }
    }
    return children;
  }

  Widget _buildBody() {
    final groupKeyOf = widget.config.groupKeyOf;
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (groupKeyOf == null) ...[
            _buildGridView(_controller.items),
          ] else ...[
            ..._buildGroupedChildren(groupKeyOf),
          ],
        ],
      ),
    );
  }

  Widget _buildPagination() {
    return PagedPaginationBar(controller: _controller);
  }
}

class _EntityGridCard<T> extends StatefulWidget {
  final T item;
  final EntityGridConfig<T> config;

  const _EntityGridCard({
    required this.item,
    required this.config,
  });

  @override
  State<_EntityGridCard<T>> createState() => _EntityGridCardState<T>();
}

class _EntityGridCardState<T> extends State<_EntityGridCard<T>> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final config = widget.config;
    final item = widget.item;
    final title = config.titleOf(item);
    final subtitle = config.subtitleOf?.call(item);
    final imageUrl = config.imageUrlOf?.call(item);
    final badge = config.badgeOf?.call(item);
    final apiClient = context.read<ApiClient>();

    return MouseRegion(
      cursor: config.onTap != null
          ? SystemMouseCursors.click
          : MouseCursor.defer,
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      child: GestureDetector(
        onTap: config.onTap != null ? () => config.onTap!(context, item) : null,
        child: Card(
          color: AppTheme.surfaceContainer,
          margin: const EdgeInsets.all(8),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
            side: const BorderSide(color: AppTheme.outline),
          ),
          clipBehavior: Clip.antiAlias,
          child: Stack(
            fit: StackFit.expand,
            children: [
              networkImageOrPlaceholder(
                imageUrl,
                apiClient,
                size: double.infinity,
                borderRadius: 0,
                placeholder: () => Container(
                  color: AppTheme.background,
                  child: Icon(
                    config.placeholderIcon,
                    size: 40,
                    color: AppTheme.textTertiary,
                  ),
                ),
              ),
              Positioned.fill(
                child: AnimatedOpacity(
                  opacity: _isHovered ? 1.0 : 0.0,
                  duration: const Duration(milliseconds: 200),
                  child: Container(
                    color: Colors.black54,
                    padding: const EdgeInsets.all(12),
                    child: Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: [
                          Text(
                            title,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 15,
                              fontWeight: FontWeight.w600,
                            ),
                            textAlign: TextAlign.center,
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                          ),
                          if (subtitle != null && subtitle.isNotEmpty) ...[
                            const SizedBox(height: 4),
                            Text(
                              subtitle,
                              style: const TextStyle(
                                color: Colors.white70,
                                fontSize: 12.5,
                              ),
                              textAlign: TextAlign.center,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ],
                          if (config.cardActions != null) ...[
                            const SizedBox(height: 8),
                            ...config.cardActions!(context, item),
                          ],
                        ],
                      ),
                    ),
                  ),
                ),
              ),
              if (badge != null && badge.isNotEmpty)
                Positioned(
                  top: 8,
                  right: 8,
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 3,
                    ),
                    decoration: BoxDecoration(
                      color: Colors.black.withValues(alpha: 0.75),
                      borderRadius: BorderRadius.circular(6),
                      border: Border.all(color: AppTheme.outline),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(
                          Icons.visibility_outlined,
                          size: 12,
                          color: AppTheme.textSecondary,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          badge,
                          style: const TextStyle(
                            color: AppTheme.textSecondary,
                            fontSize: 11,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class EntitySectionLabel extends StatelessWidget {
  final String label;

  const EntitySectionLabel(this.label, {super.key});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          Text(
            label,
            style: const TextStyle(
              color: AppTheme.textPrimary,
              fontSize: 14,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(width: 12),
          const Expanded(child: Divider(color: AppTheme.outline)),
        ],
      ),
    );
  }
}
