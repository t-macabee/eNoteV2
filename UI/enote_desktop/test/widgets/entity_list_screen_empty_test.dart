import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';

import '../helpers.dart';

void main() {
  testWidgets('empty fetch shows the no-data text', (tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: EntityListScreen<String>(
            config: EntityListConfig<String>(
              fetcher: (page, pageSize, search) async =>
                  PagedResult<String>(items: const [], totalCount: 0),
              columns: [
                ColumnSpec<String>(label: 'Naziv', value: (item) => item),
              ],
              presentation: EntityListPresentation.embedded,
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Nema podataka.'), findsOneWidget);
    expect(find.text('Nema rezultata za pretragu.'), findsNothing);
  });

  testWidgets('a search with no matches shows the no-results text', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: EntityListScreen<String>(
            config: EntityListConfig<String>(
              fetcher: (page, pageSize, search) async => search.isEmpty
                  ? PagedResult<String>(
                      items: const ['Sarajevo'],
                      page: 1,
                      pageSize: pageSize,
                      totalCount: 1,
                    )
                  : PagedResult<String>(items: const [], totalCount: 0),
              columns: [
                ColumnSpec<String>(label: 'Naziv', value: (item) => item),
              ],
              presentation: EntityListPresentation.embedded,
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
    expect(find.text('Nema podataka.'), findsNothing);
  });
}
