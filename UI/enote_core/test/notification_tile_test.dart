import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/theme/app_theme.dart';

NotificationDto _notification({required bool isRead}) => NotificationDto(
  id: 1,
  title: 'Novi ispit',
  body: 'Ispit je zakazan za petak.',
  isRead: isRead,
  createdAt: DateTime.utc(2026, 9, 1, 10),
);

Future<void> _pump(WidgetTester tester, NotificationDto notification) {
  return tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.dark,
      home: Scaffold(
        body: NotificationTile(notification: notification, onTap: () {}),
      ),
    ),
  );
}

BoxDecoration _dotDecoration(WidgetTester tester) {
  final dot = tester.widget<Container>(
    find.byWidgetPredicate(
      (widget) =>
          widget is Container &&
          widget.decoration is BoxDecoration &&
          (widget.decoration! as BoxDecoration).shape == BoxShape.circle,
    ),
  );
  return dot.decoration! as BoxDecoration;
}

void main() {
  testWidgets('NotificationTile shows the dot for an unread notification', (
    tester,
  ) async {
    await _pump(tester, _notification(isRead: false));

    final decoration = _dotDecoration(tester);
    expect(decoration.color, AppTheme.primary);
    expect(decoration.border, isNull);
  });

  testWidgets(
    'NotificationTile shows an outlined dot for a read notification',
    (tester) async {
      await _pump(tester, _notification(isRead: true));

      final decoration = _dotDecoration(tester);
      expect(decoration.color, isNull);
      expect(decoration.border, isNotNull);
    },
  );
}
