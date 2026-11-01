import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';

Future<void> _pumpTable(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: EntityListScreen<String>(
          config: EntityListConfig<String>(
            fetcher: (page, pageSize, search) async => PagedResult<String>(
              items: const ['Item 1', 'Item 2'],
              page: 1,
              pageSize: pageSize,
              totalCount: 2,
            ),
            columns: [
              ColumnSpec<String>(label: 'Naziv', value: (item) => item),
            ],
            onDelete: (context, item) async => true,
            presentation: EntityListPresentation.embedded,
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

Finder _deleteIconOpacity() => find.ancestor(
  of: find.byIcon(Icons.delete).first,
  matching: find.byType(Opacity),
);

void main() {
  testWidgets(
    'row delete icon stays in the tree at rest with reduced opacity',
    (tester) async {
      await _pumpTable(tester);

      expect(find.byIcon(Icons.delete), findsNWidgets(2));
      expect(tester.widget<Opacity>(_deleteIconOpacity().first).opacity, 0.45);
    },
  );

  testWidgets('hovering a row raises its action opacity to 1', (tester) async {
    await _pumpTable(tester);

    final gesture = await tester.createGesture(kind: PointerDeviceKind.mouse);
    addTearDown(gesture.removePointer);
    await gesture.addPointer(location: Offset.zero);
    await gesture.moveTo(tester.getCenter(find.text('Item 1')));
    await tester.pumpAndSettle();

    expect(tester.widget<Opacity>(_deleteIconOpacity().first).opacity, 1);
  });
}
