import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/features/store_employee/rental/rental_refund_dialog.dart';

double? _result;

Future<void> _pumpHarness(WidgetTester tester, {int? maxAmountCents}) async {
  _result = null;
  tester.view.physicalSize = const Size(800, 600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: Builder(
          builder: (context) => FilledButton(
            onPressed: () async {
              _result = await showRefundAmountDialog(
                context,
                maxAmountCents: maxAmountCents,
              );
            },
            child: const Text('otvori'),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.text('otvori'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('F4-12: amount above the remaining balance is rejected inline',
      (tester) async {
    await _pumpHarness(tester, maxAmountCents: 1000);

    await tester.enterText(find.byType(TextField), '15');
    await tester.pump();

    expect(
      find.text('Preostali iznos za povrat je 10.00 KM.'),
      findsOneWidget,
    );
    final confirm = find.widgetWithText(ElevatedButton, 'Potvrdi');
    expect(tester.widget<ElevatedButton>(confirm).onPressed, isNull);
  });

  testWidgets('F4-12: amount within the remaining balance stays allowed',
      (tester) async {
    await _pumpHarness(tester, maxAmountCents: 1000);

    await tester.enterText(find.byType(TextField), '5');
    await tester.pump();

    expect(
      find.text('Preostali iznos za povrat je 10.00 KM.'),
      findsNothing,
    );
    final confirm = find.widgetWithText(ElevatedButton, 'Potvrdi');
    expect(tester.widget<ElevatedButton>(confirm).onPressed, isNotNull);
  });

  testWidgets('confirm returns the amount and empty input returns 0',
      (tester) async {
    await _pumpHarness(tester, maxAmountCents: 1000);
    await tester.enterText(find.byType(TextField), '5,5');
    await tester.pump();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();
    expect(_result, 5.5);
    expect(find.byType(AlertDialog), findsNothing);

    await tester.tap(find.text('otvori'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();
    expect(_result, 0.0);
  });
}
