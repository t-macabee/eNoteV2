import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';

class _Recorder {
  final List<int> pageSizes = [];

  Future<PagedResult<String>> fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    pageSizes.add(pageSize);
    return PagedResult<String>(items: const [], totalCount: 0);
  }
}

EntityListConfig<String> _listConfig(
  _Recorder recorder, {
  EntityListPresentation presentation = EntityListPresentation.page,
}) {
  return EntityListConfig<String>(
    title: 'Gradovi',
    columns: [ColumnSpec<String>(label: 'Naziv', value: (item) => item)],
    fetcher: recorder.fetch,
    onAdd: () {},
    addLabel: 'Dodaj',
    presentation: presentation,
  );
}

Widget _wrap(Widget child) => MaterialApp(home: child);

void _useDesktopView(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

void main() {
  group('EntityListScreen toolbar layout', () {
    testWidgets(
      'page list used as a plain MaterialApp root has no AppBar and keeps '
      'its Add button in EntityToolbar',
      (tester) async {
        _useDesktopView(tester);
        final recorder = _Recorder();

        await tester.pumpWidget(
          _wrap(EntityListScreen<String>(config: _listConfig(recorder))),
        );
        await tester.pumpAndSettle();

        expect(find.byType(AppBar), findsNothing);
        expect(find.byType(EntityToolbar), findsOneWidget);
        expect(
          find.descendant(
            of: find.byType(EntityToolbar),
            matching: find.widgetWithText(ElevatedButton, 'Dodaj'),
          ),
          findsOneWidget,
        );
      },
    );

    testWidgets('pushed page list shows an AppBar with the title and a close '
        'button', (tester) async {
      _useDesktopView(tester);
      final recorder = _Recorder();

      await tester.pumpWidget(
        _wrap(
          Builder(
            builder: (context) => Scaffold(
              body: ElevatedButton(
                onPressed: () => Navigator.of(context).push(
                  MaterialPageRoute<void>(
                    builder: (_) =>
                        EntityListScreen<String>(config: _listConfig(recorder)),
                  ),
                ),
                child: const Text('Otvori'),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Otvori'));
      await tester.pumpAndSettle();

      expect(
        find.descendant(
          of: find.byType(AppBar),
          matching: find.text('Gradovi'),
        ),
        findsOneWidget,
      );
      expect(
        find.descendant(
          of: find.byType(AppBar),
          matching: find.byIcon(Icons.close),
        ),
        findsOneWidget,
      );
      expect(find.byType(EntityToolbar), findsOneWidget);
    });

    testWidgets('inline list renders EntityToolbar and drops the AppBar Add', (
      tester,
    ) async {
      final recorder = _Recorder();

      await tester.pumpWidget(
        _wrap(
          Scaffold(
            body: EntityListScreen<String>(
              config: _listConfig(
                recorder,
                presentation: EntityListPresentation.embedded,
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byType(EntityToolbar), findsOneWidget);
      expect(
        find.descendant(
          of: find.byType(EntityToolbar),
          matching: find.widgetWithText(ElevatedButton, 'Dodaj'),
        ),
        findsOneWidget,
      );
      expect(
        find.descendant(
          of: find.byType(AppBar),
          matching: find.widgetWithText(ElevatedButton, 'Dodaj'),
        ),
        findsNothing,
      );
    });

    testWidgets('embedded list has no Scaffold chrome and forces the inline '
        'toolbar', (tester) async {
      final recorder = _Recorder();

      await tester.pumpWidget(
        _wrap(
          Scaffold(
            body: EntityListScreen<String>(
              config: _listConfig(
                recorder,
                presentation: EntityListPresentation.embedded,
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byType(Scaffold), findsOneWidget);
      expect(find.byType(AppBar), findsNothing);
      expect(find.byType(EntityToolbar), findsOneWidget);
    });
  });

  group('page size', () {
    testWidgets('the list requests 20 rows a page', (tester) async {
      final recorder = _Recorder();

      await tester.pumpWidget(
        _wrap(EntityListScreen<String>(config: _listConfig(recorder))),
      );
      await tester.pumpAndSettle();

      expect(recorder.pageSizes, [20]);
    });

    testWidgets('the grid requests its config page size, defaulting to 24', (
      tester,
    ) async {
      final recorder = _Recorder();

      await tester.pumpWidget(
        _wrap(
          EntityGridScreen<String>(
            config: EntityGridConfig<String>(
              title: 'Instrumenti',
              fetcher: recorder.fetch,
              titleOf: (item) => item,
              onAdd: () {},
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(recorder.pageSizes, [24]);
      expect(find.byType(EntityToolbar), findsOneWidget);
    });
  });
}
