import 'dart:async';

import 'package:enote_core/enote_core.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/widgets/paged_fetch_controller.dart';

/// Records every (page, pageSize, search) triple the controller asks for, and
/// lets a test hold a response open so overlapping loads can be arranged.
class _RecordingFetcher {
  final List<(int, int, String)> calls = [];
  final List<Completer<PagedResult<String>>> pending = [];

  /// When set, responses resolve through a completer the test controls
  /// instead of immediately.
  bool manual = false;

  PagedResult<String> Function(int page)? responder;
  Object? throwsWith;

  Future<PagedResult<String>> call(int page, int pageSize, String search) {
    calls.add((page, pageSize, search));
    if (manual) {
      final completer = Completer<PagedResult<String>>();
      pending.add(completer);
      return completer.future;
    }
    if (throwsWith != null) {
      return Future.error(throwsWith!);
    }
    return Future.value(
      responder?.call(page) ??
          PagedResult<String>(items: ['p$page'], totalCount: 1),
    );
  }
}

/// Long enough for the controller's 300 ms search debounce to elapse.
Future<void> pastDebounce() =>
    Future<void>.delayed(const Duration(milliseconds: 400));

void main() {
  group('PagedFetchController', () {
    test('load() fetches page 1 and exposes items and total count', () async {
      final fetcher = _RecordingFetcher()
        ..responder = (page) =>
            PagedResult<String>(items: ['a', 'b'], totalCount: 7);
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 20);
      addTearDown(controller.dispose);

      await controller.load();

      expect(fetcher.calls.single, (1, 20, ''));
      expect(controller.items, ['a', 'b']);
      expect(controller.totalCount, 7);
      expect(controller.isLoading, isFalse);
    });

    test('passes its configured pageSize through unchanged', () async {
      // I1: the list uses 20 and the grid uses 24. A controller that
      // substituted a default of its own would silently repaginate one of
      // them, which no widget test would catch.
      final fetcher = _RecordingFetcher();
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 24);
      addTearDown(controller.dispose);

      await controller.load();

      expect(fetcher.calls.single.$2, 24);
    });

    test('coalesces rapid typing into a single debounced fetch', () async {
      final fetcher = _RecordingFetcher();
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 20);
      addTearDown(controller.dispose);

      await controller.load();
      fetcher.calls.clear();

      controller.searchController.text = 'g';
      controller.searchController.text = 'gi';
      controller.searchController.text = 'git';
      // Still inside the debounce window — nothing has been sent yet.
      expect(fetcher.calls, isEmpty);

      await pastDebounce();

      expect(fetcher.calls.single, (1, 20, 'git'));
      expect(controller.search, 'git');
    });

    test('a search resets to page 1', () async {
      final fetcher = _RecordingFetcher()
        ..responder = (page) =>
            PagedResult<String>(items: ['x'], totalCount: 100);
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 20);
      addTearDown(controller.dispose);

      await controller.load();
      controller.nextPage();
      await Future<void>.value();
      expect(controller.currentPage, 2);

      controller.searchController.text = 'violina';
      await pastDebounce();

      expect(controller.currentPage, 1);
      expect(fetcher.calls.last, (1, 20, 'violina'));
    });

    test('discards a stale response when a newer request is in flight',
        () async {
      final fetcher = _RecordingFetcher()..manual = true;
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 20);
      addTearDown(controller.dispose);

      // Two overlapping loads; the first one resolves last.
      final first = controller.load();
      final second = controller.load();
      expect(fetcher.pending.length, 2);

      fetcher.pending[1].complete(
        PagedResult<String>(items: ['newest'], totalCount: 1),
      );
      fetcher.pending[0].complete(
        PagedResult<String>(items: ['stale'], totalCount: 999),
      );
      await Future.wait([first, second]);

      expect(controller.items, ['newest']);
      expect(controller.totalCount, 1);
    });

    test('a stale response does not clear the loading flag', () async {
      final fetcher = _RecordingFetcher()..manual = true;
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 20);
      addTearDown(controller.dispose);

      final first = controller.load();
      final second = controller.load();

      fetcher.pending[0].complete(PagedResult<String>(items: ['stale']));
      await first;
      // The newer request is still running, so the spinner must stay up.
      expect(controller.isLoading, isTrue);

      fetcher.pending[1].complete(PagedResult<String>(items: ['newest']));
      await second;
      expect(controller.isLoading, isFalse);
    });

    test('reports a failure through onError and stops loading', () async {
      final errors = <Object>[];
      final fetcher = _RecordingFetcher()..throwsWith = ApiException('pao');
      final controller = PagedFetchController<String>(
        fetcher: fetcher.call,
        pageSize: 20,
        onError: errors.add,
      );
      addTearDown(controller.dispose);

      await controller.load();

      expect(errors.single, isA<ApiException>());
      expect(controller.isLoading, isFalse);
    });

    test('clamps totalPages to at least 1 on an empty result set', () async {
      final fetcher = _RecordingFetcher()
        ..responder = (page) => PagedResult<String>(items: [], totalCount: 0);
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 20);
      addTearDown(controller.dispose);

      expect(controller.totalPages, isNull, reason: 'nothing fetched yet');

      await controller.load();

      // "Stranica 1 od 1", never "od 0".
      expect(controller.totalPages, 1);
      expect(controller.hasNextPage, isFalse);
      expect(controller.hasPreviousPage, isFalse);
    });

    test('paging stops at both ends', () async {
      final fetcher = _RecordingFetcher()
        ..responder = (page) =>
            PagedResult<String>(items: ['p$page'], totalCount: 45);
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 20);
      addTearDown(controller.dispose);

      await controller.load();
      expect(controller.totalPages, 3);

      controller.previousPage();
      expect(controller.currentPage, 1, reason: 'already on the first page');

      controller.nextPage();
      await Future<void>.value();
      controller.nextPage();
      await Future<void>.value();
      expect(controller.currentPage, 3);

      controller.nextPage();
      expect(controller.currentPage, 3, reason: 'already on the last page');
    });

    test('refresh(resetPage: true) returns to page 1, plain refresh does not',
        () async {
      final fetcher = _RecordingFetcher()
        ..responder = (page) =>
            PagedResult<String>(items: ['p$page'], totalCount: 100);
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 20);
      addTearDown(controller.dispose);

      await controller.load();
      controller.nextPage();
      await Future<void>.value();
      expect(controller.currentPage, 2);

      controller.refresh();
      await Future<void>.value();
      expect(controller.currentPage, 2, reason: 'refresh() holds the page');

      controller.refresh(resetPage: true);
      await Future<void>.value();
      expect(controller.currentPage, 1);
    });

    test('a fetch that outlives disposal does not notify', () async {
      final fetcher = _RecordingFetcher()..manual = true;
      final controller =
          PagedFetchController<String>(fetcher: fetcher.call, pageSize: 20);

      var notifications = 0;
      controller.addListener(() => notifications++);

      final inFlight = controller.load();
      final beforeDispose = notifications;
      controller.dispose();

      fetcher.pending.single.complete(PagedResult<String>(items: ['late']));
      await expectLater(inFlight, completes);

      expect(notifications, beforeDispose);
    });
  });
}
