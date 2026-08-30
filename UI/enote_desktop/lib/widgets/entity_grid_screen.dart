import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../theme/app_theme.dart';

typedef EntityGridFetcher<T> = Future<PagedResult<T>> Function(
  int page,
  int pageSize,
  String search,
);

/// Config-driven counterpart to [EntityListConfig] / [EntityListScreen] —
/// same "config in, cards out" shape, but renders a [GridView] of
/// hover-scrim cards instead of a [DataTable]. See
/// `instrumenti_list_screen.dart` in the old eNote project for the visual
/// pattern this adapts (square card, cover image or placeholder,
/// black-scrim overlay that fades in on hover).
class EntityGridConfig<T> {
  final String title;
  final EntityGridFetcher<T> fetcher;
  final String Function(T item) titleOf;
  final String? Function(T item)? subtitleOf;
  final String? Function(T item)? imageUrlOf;
  final IconData placeholderIcon;
  final void Function(BuildContext context, T item)? onTap;
  final Future<bool?> Function(BuildContext context, T item)? onDelete;
  final VoidCallback? onAdd;
  final String? addLabel;
  final bool showAddButton;
  final String searchHint;
  final bool showSearchBar;

  /// Rendered next to the search field, in the same row. Keep it compact
  /// (e.g. wrap a dropdown in a width-bounded, left-aligned box) — it is
  /// given the remaining row width via [Expanded], not a fixed slot.
  final Widget? filterBar;

  /// Extra content rendered above the grid (e.g. a section label +
  /// divider), inside the same scrollable area. Useful for a config that
  /// composites more than one logical section under one fetch.
  final Widget? aboveGrid;

  /// Extra content rendered below the grid, inside the same scrollable
  /// area — e.g. a second, statically-rendered section.
  final Widget? belowGrid;

  final double maxCrossAxisExtent;
  final double childAspectRatio;
  final int pageSize;
  final String emptyMessage;

  const EntityGridConfig({
    required this.title,
    required this.fetcher,
    required this.titleOf,
    this.subtitleOf,
    this.imageUrlOf,
    this.placeholderIcon = Icons.image_outlined,
    this.onTap,
    this.onDelete,
    this.onAdd,
    this.addLabel = 'Dodaj',
    this.showAddButton = true,
    this.searchHint = 'Pretraži...',
    this.showSearchBar = true,
    this.filterBar,
    this.aboveGrid,
    this.belowGrid,
    this.maxCrossAxisExtent = 170,
    this.childAspectRatio = 0.85,
    this.pageSize = 24,
    this.emptyMessage = 'Nema podataka.',
  });
}

class EntityGridScreen<T> extends StatefulWidget {
  final EntityGridConfig<T> config;

  const EntityGridScreen({super.key, required this.config});

  @override
  State<EntityGridScreen<T>> createState() => EntityGridScreenState<T>();
}

class EntityGridScreenState<T> extends State<EntityGridScreen<T>> {
  late final TextEditingController _searchController;
  List<T> _items = [];
  int _currentPage = 1;
  int? _totalCount;
  bool _isLoading = false;
  String _currentSearch = '';
  Timer? _searchDebounce;
  int _requestId = 0;

  int get _pageSize => widget.config.pageSize;

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

  /// Re-runs the current page/search — call after add/edit/delete, or when
  /// external filters (a [filterBar] control) change and should re-trigger
  /// the fetch from page 1.
  void refresh({bool resetPage = false}) {
    if (resetPage) _currentPage = 1;
    _loadPage();
  }

  Future<void> _deleteItem(T item) async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrdite brisanje',
      message: 'Da li ste sigurni da želite da obrišete ovaj zapis?',
    );
    if (confirmed != true) return;

    if (!mounted) return;
    await widget.config.onDelete?.call(context, item);
    _loadPage();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.config.title)),
      body: Column(
        children: [
          _buildFilterRow(),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _items.isEmpty &&
                        widget.config.aboveGrid == null &&
                        widget.config.belowGrid == null
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
      ),
      floatingActionButton: widget.config.showAddButton
          ? FloatingActionButton.extended(
              onPressed: widget.config.onAdd,
              icon: const Icon(Icons.add),
              label: Text(widget.config.addLabel ?? 'Dodaj'),
            )
          : null,
    );
  }

  Widget _buildFilterRow() {
    final showSearch = widget.config.showSearchBar;
    final filterBar = widget.config.filterBar;
    if (!showSearch && filterBar == null) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (showSearch)
            SizedBox(
              width: 280,
              child: TextField(
                controller: _searchController,
                decoration: InputDecoration(
                  hintText: widget.config.searchHint,
                  prefixIcon: const Icon(Icons.search),
                  border: const OutlineInputBorder(),
                ),
              ),
            ),
          if (showSearch && filterBar != null) const SizedBox(width: 12),
          if (filterBar != null) Expanded(child: filterBar),
        ],
      ),
    );
  }

  Widget _buildBody() {
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (widget.config.aboveGrid != null) widget.config.aboveGrid!,
          GridView.builder(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            gridDelegate: SliverGridDelegateWithMaxCrossAxisExtent(
              maxCrossAxisExtent: widget.config.maxCrossAxisExtent,
              childAspectRatio: widget.config.childAspectRatio,
              mainAxisSpacing: 16,
              crossAxisSpacing: 16,
            ),
            itemCount: _items.length,
            itemBuilder: (context, index) {
              final item = _items[index];
              return _EntityGridCard<T>(
                item: item,
                config: widget.config,
                onDelete: widget.config.onDelete != null
                    ? () => _deleteItem(item)
                    : null,
              );
            },
          ),
          if (widget.config.belowGrid != null) widget.config.belowGrid!,
        ],
      ),
    );
  }

  Widget _buildPagination() {
    if (_totalCount == null) return const SizedBox.shrink();

    final totalPages = (_totalCount! / _pageSize).ceil().clamp(1, 1 << 30);
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

class _EntityGridCard<T> extends StatefulWidget {
  final T item;
  final EntityGridConfig<T> config;
  final VoidCallback? onDelete;

  const _EntityGridCard({
    required this.item,
    required this.config,
    this.onDelete,
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
          margin: EdgeInsets.zero,
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
                        ],
                      ),
                    ),
                  ),
                ),
              ),
              if (widget.onDelete != null)
                Positioned(
                  top: 4,
                  right: 4,
                  child: AnimatedOpacity(
                    opacity: _isHovered ? 1.0 : 0.0,
                    duration: const Duration(milliseconds: 200),
                    child: Material(
                      color: Colors.black54,
                      shape: const CircleBorder(),
                      child: SizedBox(
                        width: 28,
                        height: 28,
                        child: IconButton(
                          icon: const Icon(Icons.delete, size: 14, color: Colors.white),
                          tooltip: 'Obriši',
                          padding: EdgeInsets.zero,
                          constraints: const BoxConstraints(),
                          onPressed: widget.onDelete,
                        ),
                      ),
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
