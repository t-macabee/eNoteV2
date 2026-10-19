import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_mobile/features/payments/payment_status_view.dart';

void main() {
  testWidgets('processing explains why back is blocked', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: PaymentStatusView(
            state: PaymentFlowState.processing,
            cancelLabel: 'Nazad',
            onCancel: null,
          ),
        ),
      ),
    );

    expect(find.text('Provjeravamo status plaćanja…'), findsOneWidget);
    expect(
      find.text('Ne zatvarajte ekran, provjeravamo plaćanje.'),
      findsOneWidget,
    );
  });

  testWidgets('pending keeps its own note', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: PaymentStatusView(
            state: PaymentFlowState.pending,
            cancelLabel: 'Nazad',
            onCancel: null,
          ),
        ),
      ),
    );

    expect(
      find.text('Provjerite ponovo za nekoliko sekundi.'),
      findsOneWidget,
    );
    expect(
      find.text('Ne zatvarajte ekran, provjeravamo plaćanje.'),
      findsNothing,
    );
  });
}
