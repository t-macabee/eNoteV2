import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_mobile/widgets/blocked_reason_banner.dart';

void main() {
  testWidgets('shows the reason, keeps the sibling disabled, fires the action', (
    tester,
  ) async {
    var acted = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              BlockedReasonBanner(
                icon: Icons.warning_amber_outlined,
                reason:
                    'Imate neizmireno dugovanje od prethodnog iznajmljivanja. Izmirite ga prije novog zahtjeva.',
                actionLabel: 'Plati sada',
                onAction: () => acted = true,
              ),
              ElevatedButton(
                onPressed: null,
                onLongPress: null,
                child: const Text('Zatraži iznajmljivanje'),
              ),
            ],
          ),
        ),
      ),
    );

    expect(
      find.text(
        'Imate neizmireno dugovanje od prethodnog iznajmljivanja. Izmirite ga prije novog zahtjeva.',
      ),
      findsOneWidget,
    );
    final button = tester.widget<ElevatedButton>(
      find.widgetWithText(ElevatedButton, 'Zatraži iznajmljivanje'),
    );
    expect(button.enabled, isFalse);
    await tester.tap(find.text('Plati sada'));
    expect(acted, isTrue);
  });
}
