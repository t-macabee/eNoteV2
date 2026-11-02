import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/theme/app_theme.dart';

Future<void> _pump(WidgetTester tester, Widget banner) {
  return tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.dark,
      home: Scaffold(body: banner),
    ),
  );
}

BoxDecoration _decorationOf(WidgetTester tester) {
  final container = tester.widget<Container>(
    find
        .descendant(
          of: find.byType(StatusBanner),
          matching: find.byType(Container),
        )
        .first,
  );
  return container.decoration! as BoxDecoration;
}

void main() {
  testWidgets(
    'StatusBanner uses the tone color for the icon and the tone background',
    (tester) async {
      await _pump(
        tester,
        const StatusBanner(
          icon: Icons.check_circle_outline,
          message: 'Kurs ste završili',
          tone: StatusTone.success,
        ),
      );

      final icon = tester.widget<Icon>(find.byType(Icon));
      expect(icon.color, StatusTone.success.color);
      expect(_decorationOf(tester).color, StatusTone.success.background);
    },
  );

  testWidgets('StatusBanner shows the action button only with a label', (
    tester,
  ) async {
    await _pump(
      tester,
      const StatusBanner(icon: Icons.info_outline, message: 'Poruka'),
    );
    expect(find.byType(TextButton), findsNothing);

    await _pump(
      tester,
      StatusBanner(
        icon: Icons.info_outline,
        message: 'Poruka',
        actionLabel: 'Pokušaj ponovo',
        onAction: () {},
      ),
    );
    expect(find.text('Pokušaj ponovo'), findsOneWidget);
  });
}
