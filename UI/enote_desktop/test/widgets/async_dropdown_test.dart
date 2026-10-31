import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/widgets/async_dropdown.dart';

void main() {
  testWidgets('an empty option list shows the message and can retry', (
    tester,
  ) async {
    var calls = 0;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: AsyncDropdown<String>(
            label: 'Grad',
            fetcher: () async {
              calls++;
              return calls == 1 ? <String>[] : ['Sarajevo'];
            },
            itemLabel: (item) => item,
            itemId: (item) => item,
            onChanged: (_, _) {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Nema dostupnih opcija'), findsOneWidget);

    await tester.tap(find.byTooltip('Pokušaj ponovo'));
    await tester.pumpAndSettle();

    expect(find.text('Nema dostupnih opcija'), findsNothing);
    expect(calls, 2);

    await tester.tap(find.byType(DropdownButtonFormField<Object>));
    await tester.pumpAndSettle();
    expect(find.text('Sarajevo'), findsOneWidget);
  });

  testWidgets('emptyOptionLabel adds a null first item and applies the width', (
    tester,
  ) async {
    Object? selected = 'untouched';

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: AsyncDropdown<String>(
            label: 'Grad',
            width: 220,
            emptyOptionLabel: 'Svi gradovi',
            fetcher: () async => ['Sarajevo', 'Mostar'],
            itemLabel: (item) => item,
            itemId: (item) => item,
            onChanged: (id, _) => selected = id,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(tester.getSize(find.byType(AsyncDropdown<String>)).width, 220);
    expect(find.text('Svi gradovi'), findsOneWidget);
    expect(find.text('Nema dostupnih opcija'), findsNothing);

    await tester.tap(find.byType(DropdownButtonFormField<Object>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Mostar').last);
    await tester.pumpAndSettle();

    expect(selected, 'Mostar');

    await tester.tap(find.byType(DropdownButtonFormField<Object>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Svi gradovi').last);
    await tester.pumpAndSettle();

    expect(selected, isNull);
  });

  testWidgets('a null empty option stays selectable when the list is empty', (
    tester,
  ) async {
    Object? selected = 'untouched';

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: AsyncDropdown<String>(
            label: 'Grad',
            emptyOptionLabel: 'Svi gradovi',
            fetcher: () async => <String>[],
            itemLabel: (item) => item,
            itemId: (item) => item,
            onChanged: (id, _) => selected = id,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Svi gradovi'), findsOneWidget);
    expect(find.text('Nema dostupnih opcija'), findsNothing);

    await tester.tap(find.byType(DropdownButtonFormField<Object>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Svi gradovi').last);
    await tester.pumpAndSettle();

    expect(selected, isNull);
  });
}
