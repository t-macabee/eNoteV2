import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'package:enote_mobile/widgets/paged_list_view.dart';

import '../helpers.dart';

const _pageSize = 20;

PagedResult<int> _page(int page, {int totalCount = 60}) {
  return PagedResult<int>(
    items: [for (var i = 0; i < _pageSize; i++) (page - 1) * _pageSize + i + 1],
    page: page,
    pageSize: _pageSize,
    totalCount: totalCount,
  );
}

PagedFetchController<int> _controller(
  Future<PagedResult<int>> Function(int page, int pageSize, String search)
  fetcher,
) {
  return PagedFetchController<int>(fetcher: fetcher, pageSize: _pageSize);
}

Future<void> _pumpList(
  WidgetTester tester,
  PagedFetchController<int> controller, {
  Widget? statusTabs,
}) async {
  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: PagedListView<int>(
          controller: controller,
          statusTabs: statusTabs,
          itemBuilder: (context, item) => ListTile(title: Text('Item $item')),
        ),
      ),
    ),
  );
  await tester.pump();
  await tester.pump();
}

void main() {
  testWidgets('search debounce resets to page 1', (tester) async {
    final calls = <Map<String, Object?>>[];
    final controller = _controller((page, pageSize, search) async {
      calls.add({'page': page, 'search': search});
      return PagedResult<int>(
        items: const [],
        page: page,
        pageSize: pageSize,
        totalCount: 0,
      );
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);
    calls.clear();

    await tester.enterText(find.byType(TextField), 'abc');
    await tester.pump();
    expect(calls, isEmpty);
    await pumpPastDebounce(tester);
    await tester.pump();
    expect(calls.length, 1);
    expect(calls.first['page'], 1);
    expect(calls.first['search'], 'abc');
  });

  testWidgets('scrolling to the end loads the next page', (tester) async {
    final requestedPages = <int>[];
    final controller = _controller((page, pageSize, search) async {
      requestedPages.add(page);
      return _page(page);
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);
    expect(find.text('Item 21'), findsNothing);

    await tester.drag(find.byType(ListView), const Offset(0, -5000));
    await tester.pumpAndSettle();

    expect(requestedPages, containsAllInOrder([1, 2]));
    expect(find.text('Item 21'), findsOneWidget);
  });

  testWidgets('footer shows a retry row after a failed loadMore', (
    tester,
  ) async {
    var fail = false;
    final controller = _controller((page, pageSize, search) async {
      if (fail) throw Exception('boom');
      return _page(page);
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);

    fail = true;
    await tester.drag(find.byType(ListView), const Offset(0, -5000));
    await tester.pumpAndSettle();

    expect(find.text('Učitavanje nije uspjelo.'), findsOneWidget);
    expect(find.text('Pokušaj ponovo'), findsOneWidget);
    expect(find.text('Item 1'), findsNothing, reason: 'scrolled to the end');
    expect(controller.items.length, _pageSize);
  });

  testWidgets('empty message mentions the search only when searching', (
    tester,
  ) async {
    final controller = _controller((page, pageSize, search) async {
      return PagedResult<int>(
        items: const [],
        page: page,
        pageSize: pageSize,
        totalCount: 0,
      );
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);
    expect(find.text('Nema podataka.'), findsOneWidget);
    expect(find.text('Nema rezultata za pretragu.'), findsNothing);

    await tester.enterText(find.byType(TextField), 'abc');
    await pumpPastDebounce(tester);
    await tester.pumpAndSettle();

    expect(find.text('Nema rezultata za pretragu.'), findsOneWidget);
    expect(find.text('Nema podataka.'), findsNothing);
  });

  testWidgets('status tabs sit between the search field and the rows', (
    tester,
  ) async {
    final controller = _controller((page, pageSize, search) async {
      return _page(page, totalCount: 2);
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller, statusTabs: const Text('Tabs'));

    final searchBottom = tester.getBottomLeft(find.byType(TextField)).dy;
    final tabsTop = tester.getTopLeft(find.text('Tabs')).dy;
    final tabsBottom = tester.getBottomLeft(find.text('Tabs')).dy;
    final firstRowTop = tester.getTopLeft(find.text('Item 1')).dy;
    expect(tabsTop, greaterThanOrEqualTo(searchBottom));
    expect(firstRowTop, greaterThanOrEqualTo(tabsBottom));
  });

  testWidgets('list has no pagination bar', (tester) async {
    final controller = _controller((page, pageSize, search) async {
      return _page(page);
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);

    expect(find.byType(PagedPaginationBar), findsNothing);
    expect(find.text('Sljedeća'), findsNothing);
  });

  testWidgets('a failed load shows retry and the retry refetches', (
    tester,
  ) async {
    var calls = 0;
    final controller = _controller((page, pageSize, search) async {
      calls++;
      throw Exception('boom');
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);
    expect(find.text('Pokušaj ponovo'), findsOneWidget);

    await tester.tap(find.text('Pokušaj ponovo'));
    await tester.pump();
    await tester.pump();
    expect(calls, 2);
  });

  testWidgets('an error on top of loaded rows keeps the rows visible', (
    tester,
  ) async {
    var fail = false;
    final controller = _controller((page, pageSize, search) async {
      if (fail) throw Exception('boom');
      return PagedResult<int>(
        items: const [1, 2],
        page: page,
        pageSize: pageSize,
        totalCount: 2,
      );
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);

    fail = true;
    controller.load();
    await tester.pump();
    await tester.pump();

    expect(find.text('Item 1'), findsOneWidget);
    expect(find.text('Item 2'), findsOneWidget);
    expect(find.text('Pokušaj ponovo'), findsNothing);
  });
}
