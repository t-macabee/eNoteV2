import 'dart:async';

import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

/// Fetches one page of [T]. Structurally identical to `EntityFetcher` and
/// `EntityGridFetcher` — those typedefs stay where they are so the two config
/// classes keep their own vocabulary.
typedef PagedFetcher<T> = Future<PagedResult<T>> Function(
  int page,
  int pageSize,
  String search,
);

/// The paged load / search-debounce / stale-response state machine shared by
/// [EntityListScreen] and [EntityGridScreen], which previously carried one
/// hand-copied instance each.
///
/// Owns exactly the behaviour the two screens agreed on: the current page, the
/// loaded items, the total count, the loading flag, the search field and its
/// 300 ms debounce, and the request-id guard that drops a response whose
/// request has already been superseded.
///
/// It deliberately owns nothing presentational. Delete confirmation, the empty
/// state, the toolbar layout and the page body differ between the two screens
/// on purpose (a `DataTable` and a `GridView` are not the same thing), so those
/// stay in each widget's `build`.
///
/// Errors are surfaced through [onError] rather than shown here: an
/// `ErrorBanner` needs a [BuildContext] and a [ChangeNotifier] has none. The
/// callback fires only for a response that is still current, and the owning
/// widget is responsible for its own `mounted` check.
class PagedFetchController<T> extends ChangeNotifier {
  PagedFetchController({
    required this.fetcher,
    required this.pageSize,
    this.onError,
    this.searchDebounceDuration = const Duration(milliseconds: 300),
  }) {
    searchController.addListener(_onSearchChanged);
  }

  /// Called with the page/pageSize/search triple for every load.
  ///
  /// Owning widgets should pass a closure that reads `widget.config.fetcher` at
  /// call time rather than the fetcher itself, so a rebuilt config is picked up
  /// — several screens rebuild their config on every `build` with closures that
  /// read filter state as fields.
  final PagedFetcher<T> fetcher;

  /// Rows requested per page. No default on purpose: [EntityListScreen] uses
  /// 20 and [EntityGridScreen] uses `config.pageSize` (24). A default here
  /// would silently repaginate one of them.
  final int pageSize;

  /// Invoked when a still-current load throws.
  final void Function(Object error)? onError;

  final Duration searchDebounceDuration;

  final TextEditingController searchController = TextEditingController();

  List<T> _items = [];
  int _currentPage = 1;
  int? _totalCount;
  bool _isLoading = false;
  String _currentSearch = '';
  Timer? _searchDebounce;
  int _requestId = 0;
  bool _disposed = false;

  List<T> get items => _items;
  int get currentPage => _currentPage;
  int? get totalCount => _totalCount;
  bool get isLoading => _isLoading;
  String get search => _currentSearch;

  /// Null until the first load resolves — callers hide pagination while it is.
  ///
  /// Clamped to at least 1 so an empty result set reads "Stranica 1 od 1"
  /// rather than "Stranica 1 od 0".
  int? get totalPages => _totalCount == null
      ? null
      : (_totalCount! / pageSize).ceil().clamp(1, 1 << 30);

  bool get hasPreviousPage => _currentPage > 1;

  bool get hasNextPage {
    final pages = totalPages;
    return pages != null && _currentPage < pages;
  }

  void _onSearchChanged() {
    final value = searchController.text;
    _searchDebounce?.cancel();
    _searchDebounce = Timer(searchDebounceDuration, () {
      if (value != _currentSearch) {
        _currentSearch = value;
        _currentPage = 1;
        load();
      }
    });
  }

  /// Loads the current page. Concurrent calls are serialised by request id:
  /// only the newest one is allowed to write back.
  Future<void> load() async {
    final requestId = ++_requestId;
    _isLoading = true;
    _notify();

    try {
      final result = await fetcher(_currentPage, pageSize, _currentSearch);
      if (requestId != _requestId) return;
      _items = result.items;
      _totalCount = result.totalCount;
      _notify();
    } catch (e) {
      if (requestId != _requestId) return;
      onError?.call(e);
    } finally {
      if (requestId == _requestId && !_disposed) {
        _isLoading = false;
        _notify();
      }
    }
  }

  /// Re-runs the current page/search — call after add/edit/delete, or when an
  /// external filter changes and should re-trigger the fetch from page 1.
  void refresh({bool resetPage = false}) {
    if (resetPage) _currentPage = 1;
    load();
  }

  void previousPage() {
    if (!hasPreviousPage) return;
    _currentPage--;
    load();
  }

  void nextPage() {
    if (!hasNextPage) return;
    _currentPage++;
    load();
  }

  /// The [State.mounted] equivalent for a listenable: a fetch can outlive the
  /// widget that started it, and notifying after disposal throws.
  void _notify() {
    if (_disposed) return;
    notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    _searchDebounce?.cancel();
    searchController.removeListener(_onSearchChanged);
    searchController.dispose();
    super.dispose();
  }
}
