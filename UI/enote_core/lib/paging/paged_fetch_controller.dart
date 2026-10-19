import 'dart:async';

import 'package:flutter/material.dart';
import 'paged_result.dart';

typedef PagedFetcher<T> = Future<PagedResult<T>> Function(
  int page,
  int pageSize,
  String search,
);

class PagedFetchController<T> extends ChangeNotifier {
  static const Duration defaultSearchDebounce = Duration(milliseconds: 300);

  PagedFetchController({
    required this.fetcher,
    required this.pageSize,
    this.onError,
    this.searchDebounceDuration = defaultSearchDebounce,
  }) {
    searchController.addListener(_onSearchChanged);
  }

  final PagedFetcher<T> fetcher;

  final int pageSize;

  final void Function(Object error)? onError;

  final Duration searchDebounceDuration;

  final TextEditingController searchController = TextEditingController();

  List<T> _items = [];
  int _currentPage = 1;
  int? _totalCount;
  bool _isLoading = false;
  Object? _error;
  String _currentSearch = '';
  Timer? _searchDebounce;
  int _requestId = 0;
  bool _disposed = false;

  List<T> get items => _items;
  int get currentPage => _currentPage;
  int? get totalCount => _totalCount;
  bool get isLoading => _isLoading;

  Object? get error => _error;

  String get search => _currentSearch;

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

  Future<void> load() async {
    final requestId = ++_requestId;
    _isLoading = true;
    _notify();

    try {
      final result = await fetcher(_currentPage, pageSize, _currentSearch);
      if (requestId != _requestId) return;
      _items = result.items;
      _totalCount = result.totalCount;
      _error = null;
      _notify();
    } catch (e) {
      if (requestId != _requestId) return;
      _error = e;
      onError?.call(e);
    } finally {
      if (requestId == _requestId && !_disposed) {
        _isLoading = false;
        _notify();
      }
    }
  }

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

class PagedPaginationBar extends StatelessWidget {
  final PagedFetchController controller;

  const PagedPaginationBar({super.key, required this.controller});

  @override
  Widget build(BuildContext context) {
    final totalPages = controller.totalPages;
    if (totalPages == null) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.all(16),
      child: Wrap(
        alignment: WrapAlignment.center,
        crossAxisAlignment: WrapCrossAlignment.center,
        spacing: 16,
        runSpacing: 8,
        children: [
          Text('Stranica ${controller.currentPage} od $totalPages'),
          Text('Ukupno: ${controller.totalCount}'),
          TextButton.icon(
            onPressed: controller.hasPreviousPage
                ? controller.previousPage
                : null,
            icon: const Icon(Icons.chevron_left),
            label: const Text('Prethodna'),
          ),
          TextButton.icon(
            onPressed: controller.hasNextPage ? controller.nextPage : null,
            icon: const Icon(Icons.chevron_right),
            label: const Text('Sljedeća'),
          ),
        ],
      ),
    );
  }
}
