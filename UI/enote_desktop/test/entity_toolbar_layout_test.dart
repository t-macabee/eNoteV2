import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';
import 'package:enote_desktop/widgets/entity_toolbar.dart';

/// Guards the toolbar and page-size invariants that [EntityListScreen] and
/// [EntityGridScreen] do *not* share, and which the extraction of
/// [EntityToolbar] and `PagedFetchController` out of the two screens could
/// otherwise flatten without failing anything.
///
/// The two screens look like twins but differ deliberately:
///  * the list has a second, stacked toolbar layout (search bar above the
///    filter bar, Add button in the AppBar actions) that the grid has no
///    counterpart for, and that every default list screen uses;
///  * the list paginates at 20 rows, the grid at 24.

/// Records the page size each screen asks for.
class _Recorder {
  final List<int> pageSizes = [];

  Future<PagedResult<String>> fetch(int page, int pageSize, String search) async {
    pageSizes.add(pageSize);
    return PagedResult<String>(items: const [], totalCount: 0);
  }
}

EntityListConfig<String> _listConfig(
  _Recorder recorder, {
  bool inlineToolbar = false,
  EntityListPresentation presentation = EntityListPresentation.page,
}) {
  return EntityListConfig<String>(
    title: 'Gradovi',
    columns: [ColumnSpec<String>(label: 'Naziv', value: (item) => item)],
    fetcher: recorder.fetch,
    onAdd: () {},
    addLabel: 'Dodaj',
    inlineToolbar: inlineToolbar,
    presentation: presentation,
  );
}

Widget _wrap(Widget child) => MaterialApp(home: child);

void main() {
  group('EntityListScreen toolbar layout', () {
    testWidgets(
        'default (non-inline) list keeps its Add button in the AppBar and uses '
        'the stacked search bar, not EntityToolbar', (tester) async {
      final recorder = _Recorder();

      await tester.pumpWidget(
        _wrap(EntityListScreen<String>(config: _listConfig(recorder))),
      );
      await tester.pumpAndSettle();

      // This is the regression the extraction most plausibly introduces:
      // treating the toolbar as fully shared moves Add out of the AppBar and
      // every default list screen loses its Add button.
      expect(
        find.descendant(
          of: find.byType(AppBar),
          matching: find.widgetWithText(ElevatedButton, 'Dodaj'),
        ),
        findsOneWidget,
      );
      expect(find.byType(EntityToolbar), findsNothing);
      expect(find.byType(TextField), findsOneWidget);
    });

    testWidgets('inline list renders EntityToolbar and drops the AppBar Add',
        (tester) async {
      final recorder = _Recorder();

      await tester.pumpWidget(
        _wrap(
          EntityListScreen<String>(
            config: _listConfig(recorder, inlineToolbar: true),
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

      // Only the host screen's Scaffold, and no AppBar of the list's own.
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

    testWidgets('the grid requests its config page size, defaulting to 24',
        (tester) async {
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
