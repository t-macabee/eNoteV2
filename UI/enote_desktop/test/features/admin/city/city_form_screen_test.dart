import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/city/city_form_screen.dart';

const _reason = 'Grad se ne može obrisati jer je u upotrebi.';

void main() {
  testWidgets(
    'a city form with a blocked delete shows a disabled delete with the reason',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(
        MaterialApp(
          home: CityFormScreen(
            existing: CityDto(
              id: 1,
              name: 'Sarajevo',
              deleteBlockedReason: _reason,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      final deleteButton = find.ancestor(
        of: find.text('Obriši'),
        matching: find.byType(OutlinedButton),
      );

      expect(deleteButton, findsOneWidget);
      expect(
        tester.widget<OutlinedButton>(deleteButton).onPressed,
        isNull,
      );
      expect(find.byTooltip(_reason), findsOneWidget);
    },
  );
}
