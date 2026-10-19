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
}
