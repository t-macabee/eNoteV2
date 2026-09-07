import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_transition_action_row.dart';

void main() {
  group('RentalTransitionActionRow', () {
    testWidgets('Cancel button renders when status is approved', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: RentalTransitionActionRow(
              status: InstrumentRentalStatus.approved,
              onTransition: (trigger, {note}) async {},
            ),
          ),
        ),
      );

      expect(find.widgetWithText(ElevatedButton, 'Preuzeto'), findsOneWidget);
      expect(find.widgetWithText(ElevatedButton, 'Otkaži'), findsOneWidget);
    });

    testWidgets('Cancel button does NOT render when status is pending', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: RentalTransitionActionRow(
              status: InstrumentRentalStatus.pending,
              onTransition: (trigger, {note}) async {},
            ),
          ),
        ),
      );

      expect(find.widgetWithText(ElevatedButton, 'Odobri'), findsOneWidget);
      expect(find.widgetWithText(ElevatedButton, 'Odbij'), findsOneWidget);
      expect(find.widgetWithText(ElevatedButton, 'Otkaži'), findsNothing);
    });

    testWidgets('Cancel button does NOT render when status is active', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: RentalTransitionActionRow(
              status: InstrumentRentalStatus.active,
              onTransition: (trigger, {note}) async {},
            ),
          ),
        ),
      );

      expect(find.widgetWithText(ElevatedButton, 'Završi'), findsOneWidget);
      expect(find.widgetWithText(ElevatedButton, 'Prijevremeni povrat'), findsOneWidget);
      expect(find.widgetWithText(ElevatedButton, 'Otkaži'), findsNothing);
    });

    for (final terminalStatus in [
      InstrumentRentalStatus.completed,
      InstrumentRentalStatus.canceled,
      InstrumentRentalStatus.rejected,
      InstrumentRentalStatus.returnedEarly,
    ]) {
      testWidgets('Cancel button does NOT render when status is $terminalStatus', (tester) async {
        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              body: RentalTransitionActionRow(
                status: terminalStatus,
                onTransition: (trigger, {note}) async {},
              ),
            ),
          ),
        );

        expect(find.widgetWithText(ElevatedButton, 'Otkaži'), findsNothing);
      });
    }

    testWidgets('Tapping Cancel prompts for mandatory note before calling onTransition', (tester) async {
      RentalTrigger? calledTrigger;
      String? calledNote;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: RentalTransitionActionRow(
              status: InstrumentRentalStatus.approved,
              onTransition: (trigger, {note}) async {
                calledTrigger = trigger;
                calledNote = note;
              },
            ),
          ),
        ),
      );

      await tester.tap(find.widgetWithText(ElevatedButton, 'Otkaži'));
      await tester.pumpAndSettle();

      // Reason dialog is shown with title 'Otkaži'
      expect(find.byType(AlertDialog), findsOneWidget);
      expect(find.text('Razlog *'), findsOneWidget);

      // Confirm button is initially disabled because note is empty
      final confirmButton = tester.widget<ElevatedButton>(find.widgetWithText(ElevatedButton, 'Potvrdi'));
      expect(confirmButton.onPressed, isNull);

      // Enter a reason
      await tester.enterText(find.byType(TextField), 'Student se nije pojavio');
      await tester.pumpAndSettle();

      // Confirm button is now enabled
      await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
      await tester.pumpAndSettle();

      expect(calledTrigger, RentalTrigger.cancel);
      expect(calledNote, 'Student se nije pojavio');
    });
  });
}
