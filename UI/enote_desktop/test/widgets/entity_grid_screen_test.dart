import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_empty_state.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';

import '../helpers.dart';

Future<void> _pumpGrid(
  WidgetTester tester, {
  required List<String> items,
  ({String label, StatusTone tone})? Function(String item)? statusOf,
}) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: ScriptedClient((_) => jsonResponse('', 200)),
  );

  await tester.pumpWidget(
    Provider<ApiClient>.value(
      value: apiClient,
      child: MaterialApp(
        home: Scaffold(
          body: EntityGridScreen<String>(
            config: EntityGridConfig<String>(
              fetcher: (page, pageSize, search) async => PagedResult<String>(
                items: items,
                page: 1,
                pageSize: 24,
                totalCount: items.length,
              ),
              titleOf: (item) => item,
              statusOf: statusOf,
            ),
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('card shows the title without hover', (
    WidgetTester tester,
  ) async {
    await _pumpGrid(tester, items: const ['Sarajevo']);

    expect(find.text('Sarajevo'), findsOneWidget);
    expect(
      find.descendant(
        of: find.byType(AnimatedOpacity),
        matching: find.text('Sarajevo'),
      ),
      findsNothing,
    );
  });

  testWidgets('card shows the status chip from statusOf', (
    WidgetTester tester,
  ) async {
    await _pumpGrid(
      tester,
      items: const ['Sarajevo'],
      statusOf: (item) => (label: 'Neaktivan', tone: StatusTone.neutral),
    );

    expect(find.byType(StatusChip), findsOneWidget);
    expect(find.text('Neaktivan'), findsOneWidget);
  });
  testWidgets(
    'shows EntityEmptyState with the no-data text on an empty result',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: ScriptedClient((_) => jsonResponse('', 200)),
      );

      await tester.pumpWidget(
        Provider<ApiClient>.value(
          value: apiClient,
          child: MaterialApp(
            home: Scaffold(
              body: EntityGridScreen<String>(
                config: EntityGridConfig<String>(
                  fetcher: (page, pageSize, search) async =>
                      PagedResult<String>(
                        items: const [],
                        page: 1,
                        pageSize: 24,
                        totalCount: 0,
                      ),
                  titleOf: (item) => item,
                ),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byType(EntityEmptyState), findsOneWidget);
      expect(find.text('Nema podataka.'), findsOneWidget);
    },
  );

  testWidgets(
    'shows the search text in the empty state after a search with no result',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: ScriptedClient((_) => jsonResponse('', 200)),
      );

      await tester.pumpWidget(
        Provider<ApiClient>.value(
          value: apiClient,
          child: MaterialApp(
            home: Scaffold(
              body: EntityGridScreen<String>(
                config: EntityGridConfig<String>(
                  fetcher: (page, pageSize, search) async => search.isEmpty
                      ? PagedResult<String>(
                          items: const ['Sarajevo'],
                          page: 1,
                          pageSize: pageSize,
                          totalCount: 1,
                        )
                      : PagedResult<String>(items: const [], totalCount: 0),
                  titleOf: (item) => item,
                ),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Sarajevo'), findsOneWidget);

      await tester.enterText(find.byType(TextField), 'xyz');
      await pumpPastDebounce(tester);
      await tester.pumpAndSettle();

      expect(find.text('Nema rezultata za pretragu.'), findsOneWidget);
    },
  );

  testWidgets(
    'builds no AppBar when the screen cannot pop, even with a title',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: ScriptedClient((_) => jsonResponse('', 200)),
      );

      await tester.pumpWidget(
        Provider<ApiClient>.value(
          value: apiClient,
          child: MaterialApp(
            home: EntityGridScreen<String>(
              config: EntityGridConfig<String>(
                title: 'Grid Title',
                fetcher: (page, pageSize, search) async => PagedResult<String>(
                  items: const [],
                  page: 1,
                  pageSize: 24,
                  totalCount: 0,
                ),
                titleOf: (item) => item,
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byType(AppBar), findsNothing);
      expect(find.text('Grid Title'), findsNothing);
    },
  );

  testWidgets(
    'builds an AppBar with a close button when the screen is pushed',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: ScriptedClient((_) => jsonResponse('', 200)),
      );

      await tester.pumpWidget(
        Provider<ApiClient>.value(
          value: apiClient,
          child: MaterialApp(
            home: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () => Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => EntityGridScreen<String>(
                      config: EntityGridConfig<String>(
                        title: 'Grid Title',
                        fetcher: (page, pageSize, search) async =>
                            PagedResult<String>(
                              items: const [],
                              page: 1,
                              pageSize: 24,
                              totalCount: 0,
                            ),
                        titleOf: (item) => item,
                      ),
                    ),
                  ),
                ),
                child: const Text('push'),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('push'));
      await tester.pumpAndSettle();

      expect(find.byType(AppBar), findsOneWidget);
      expect(find.text('Grid Title'), findsOneWidget);

      await tester.tap(find.byTooltip('Zatvori'));
      await tester.pumpAndSettle();

      expect(find.byType(EntityGridScreen<String>), findsNothing);
    },
  );

  testWidgets('embedded grid builds no Scaffold of its own', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: ScriptedClient((_) => jsonResponse('', 200)),
    );

    await tester.pumpWidget(
      Provider<ApiClient>.value(
        value: apiClient,
        child: MaterialApp(
          home: Material(
            child: Column(
              children: [
                SizedBox(
                  height: 400,
                  child: EntityGridScreen<String>(
                    config: EntityGridConfig<String>(
                      embedded: true,
                      fetcher: (page, pageSize, search) async =>
                          PagedResult<String>(
                            items: const [],
                            page: 1,
                            pageSize: 24,
                            totalCount: 0,
                          ),
                      titleOf: (item) => item,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(Scaffold), findsNothing);
  });

  testWidgets('caps content width at AppSize.contentMaxWidth', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: ScriptedClient((_) => jsonResponse('', 200)),
    );

    await tester.pumpWidget(
      Provider<ApiClient>.value(
        value: apiClient,
        child: MaterialApp(
          home: EntityGridScreen<String>(
            config: EntityGridConfig<String>(
              fetcher: (page, pageSize, search) async => PagedResult<String>(
                items: const [],
                page: 1,
                pageSize: 24,
                totalCount: 0,
              ),
              titleOf: (item) => item,
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final constrainedBox = find.byWidgetPredicate(
      (w) =>
          w is ConstrainedBox &&
          w.constraints.maxWidth == AppSize.contentMaxWidth,
    );
    final size = tester.getSize(constrainedBox);
    expect(size.width, lessThanOrEqualTo(AppSize.contentMaxWidth));
  });

  testWidgets('grouped grid renders one EntitySectionLabel per group', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: ScriptedClient((_) => jsonResponse('', 200)),
    );

    await tester.pumpWidget(
      Provider<ApiClient>.value(
        value: apiClient,
        child: MaterialApp(
          home: Scaffold(
            body: EntityGridScreen<String>(
              config: EntityGridConfig<String>(
                fetcher: (page, pageSize, search) async => PagedResult<String>(
                  items: const ['Apple', 'Apricot', 'Banana'],
                  page: 1,
                  pageSize: 24,
                  totalCount: 3,
                ),
                titleOf: (item) => item,
                groupKeyOf: (item) => item[0],
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(EntitySectionLabel), findsNWidgets(2));
  });
}
