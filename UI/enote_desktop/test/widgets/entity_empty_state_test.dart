import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/widgets/entity_empty_state.dart';

void main() {
  testWidgets('shows the no-data text when not searching', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: EntityEmptyState(searching: false)),
      ),
    );

    expect(find.text('Nema podataka.'), findsOneWidget);
    expect(find.text('Nema rezultata za pretragu.'), findsNothing);
    expect(find.byIcon(Icons.inbox_outlined), findsOneWidget);
  });

  testWidgets('shows the no-results text when searching', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: EntityEmptyState(searching: true)),
      ),
    );

    expect(find.text('Nema rezultata za pretragu.'), findsOneWidget);
    expect(find.text('Nema podataka.'), findsNothing);
  });
}
