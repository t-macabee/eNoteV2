import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';
import 'entity_empty_state.dart';

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
  final ({String label, StatusTone tone})? Function(T item)? statusOf;
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

  final Widget? statusTabs;

  final double maxCrossAxisExtent;
  final double childAspectRatio;
  final int pageSize;

  final String Function(T item)? groupKeyOf;

  const EntityGridConfig({
    this.title,
    required this.fetcher,
    required this.titleOf,
    this.subtitleOf,
    this.imageUrlOf,
    this.placeholderIcon = Icons.image_outlined,
    this.statusOf,
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
    this.statusTabs,
    this.maxCrossAxisExtent = 220,
    this.childAspectRatio = 0.85,
    this.pageSize = 24,
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
      body: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: AppSize.contentMaxWidth),
          child: content,
        ),
      ),
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
          statusTabs: widget.config.statusTabs,
          trailing: widget.config.trailing,
          showAdd: widget.config.showAddButton && widget.config.onAdd != null,
          onAdd: widget.config.onAdd,
          addLabel: widget.config.addLabel,
        ),
        Expanded(
          child: _controller.isLoading
              ? const Center(child: CircularProgressIndicator())
              : _controller.items.isEmpty
              ? EntityEmptyState(
                  searching: _controller.searchController.text
                      .trim()
                      .isNotEmpty,
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
        mainAxisSpacing: AppSpacing.xl,
        crossAxisSpacing: AppSpacing.xl,
      ),
      itemCount: items.length,
      itemBuilder: (context, index) {
        final item = items[index];
        return _EntityGridCard<T>(item: item, config: widget.config);
      },
    );
  }

  List<Widget> _buildGroupedChildren(String Function(T item) groupKeyOf) {
    final groups = <String, List<T>>{};
    for (final item in _controller.items) {
      groups.putIfAbsent(groupKeyOf(item), () => []).add(item);
    }

    final sortedKeys = groups.keys.toList()
      ..sort((a, b) => a.toLowerCase().compareTo(b.toLowerCase()));

    final children = <Widget>[];
    for (var i = 0; i < sortedKeys.length; i++) {
      final key = sortedKeys[i];
      children.addAll([EntitySectionLabel(key), _buildGridView(groups[key]!)]);
      if (i != sortedKeys.length - 1) {
        children.add(const SizedBox(height: AppSpacing.xl));
      }
    }
    return children;
  }

  Widget _buildBody() {
    final groupKeyOf = widget.config.groupKeyOf;
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        0,
        AppSpacing.lg,
        AppSpacing.lg,
      ),
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
    return Container(
      decoration: const BoxDecoration(
        border: Border(top: BorderSide(color: AppTheme.outline, width: 1)),
      ),
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
      child: PagedPaginationBar(controller: _controller),
    );
  }
}

class _EntityGridCard<T> extends StatefulWidget {
  final T item;
  final EntityGridConfig<T> config;

  const _EntityGridCard({required this.item, required this.config});

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
    final status = config.statusOf?.call(item);
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
          margin: const EdgeInsets.all(AppSpacing.sm),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppRadius.lg),
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
              Positioned(
                left: 0,
                right: 0,
                bottom: 0,
                child: Container(
                  padding: const EdgeInsets.all(AppSpacing.md),
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                      colors: [
                        Colors.transparent,
                        AppTheme.background.withValues(alpha: 0.85),
                      ],
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        title,
                        style: Theme.of(context).textTheme.titleSmall
                            ?.copyWith(color: AppTheme.textPrimary),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      if (subtitle != null && subtitle.isNotEmpty) ...[
                        const SizedBox(height: AppSpacing.xs),
                        Text(
                          subtitle,
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: AppTheme.textSecondary),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              if (config.cardActions != null)
                Positioned.fill(
                  child: AnimatedOpacity(
                    opacity: _isHovered ? 1.0 : 0.0,
                    duration: const Duration(milliseconds: 200),
                    child: Container(
                      color: AppTheme.background.withValues(alpha: 0.6),
                      padding: const EdgeInsets.all(AppSpacing.md),
                      child: Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: [...config.cardActions!(context, item)],
                        ),
                      ),
                    ),
                  ),
                ),
              if (status != null)
                Positioned(
                  top: 8,
                  right: 8,
                  child: StatusChip(label: status.label, tone: status.tone),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

({String label, StatusTone tone})? inactiveStatus(bool isActive) =>
    isActive ? null : (label: 'Neaktivan', tone: StatusTone.neutral);

({String label, StatusTone tone})? unpublishedStatus(bool isPublished) =>
    isPublished ? null : (label: 'Neobjavljen', tone: StatusTone.warning);

class EntitySectionLabel extends StatelessWidget {
  final String label;

  const EntitySectionLabel(this.label, {super.key});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Row(
        children: [
          Text(
            label,
            style: Theme.of(context).textTheme.titleSmall
                ?.copyWith(color: AppTheme.textPrimary),
          ),
          const SizedBox(width: AppSpacing.md),
          const Expanded(child: Divider(color: AppTheme.outline)),
        ],
      ),
    );
  }
}
