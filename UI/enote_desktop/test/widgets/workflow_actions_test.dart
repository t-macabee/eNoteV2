import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/workflow_actions.dart';

const _approve = WorkflowAction<RentalTrigger>(
  trigger: RentalTrigger.approve,
  label: 'Odobri',
  icon: Icons.check,
  confirmMessage: 'Odobriti zahtjev?',
);

const _reject = WorkflowAction<RentalTrigger>(
  trigger: RentalTrigger.reject,
  label: 'Odbij',
  icon: Icons.cancel_outlined,
  tone: StatusTone.danger,
  requiresNote: true,
);

const _plain = WorkflowAction<RentalTrigger>(
  trigger: RentalTrigger.pickup,
  label: 'Preuzeto',
  icon: Icons.inventory_2_outlined,
);

Future<void> _pumpButtons(
  WidgetTester tester, {
  required List<WorkflowAction<RentalTrigger>> actions,
  required Future<void> Function(RentalTrigger trigger, {String? note}) onRun,
  bool compact = false,
}) {
  return tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: Center(
          child: WorkflowActionButtons<RentalTrigger>(
            actions: actions,
            onRun: onRun,
            compact: compact,
          ),
        ),
      ),
    ),
  );
}

void main() {
  testWidgets('action with a note prompts for a reason', (tester) async {
    RentalTrigger? calledTrigger;
    String? calledNote;
    await _pumpButtons(
      tester,
      actions: const [_reject],
      onRun: (trigger, {note}) async {
        calledTrigger = trigger;
        calledNote = note;
      },
    );

    await tester.tap(find.widgetWithText(ElevatedButton, 'Odbij'));
    await tester.pumpAndSettle();

    expect(find.text('Razlog *'), findsOneWidget);
    await tester.enterText(find.byType(TextField), 'Instrument je oštećen');
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(calledTrigger, RentalTrigger.reject);
    expect(calledNote, 'Instrument je oštećen');
  });

  testWidgets('action with a confirm message asks for confirmation', (
    tester,
  ) async {
    var calls = 0;
    await _pumpButtons(
      tester,
      actions: const [_approve],
      onRun: (trigger, {note}) async => calls++,
    );

    await tester.tap(find.widgetWithText(ElevatedButton, 'Odobri'));
    await tester.pumpAndSettle();

    expect(find.text('Odobriti zahtjev?'), findsOneWidget);
    expect(calls, 0);

    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(calls, 1);
  });

  testWidgets('action without note or confirm runs at once', (tester) async {
    var calls = 0;
    await _pumpButtons(
      tester,
      actions: const [_plain],
      onRun: (trigger, {note}) async => calls++,
    );

    await tester.tap(find.widgetWithText(ElevatedButton, 'Preuzeto'));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsNothing);
    expect(calls, 1);
  });

  testWidgets('dialog title falls back to the label', (tester) async {
    await _pumpButtons(
      tester,
      actions: const [_approve],
      onRun: (trigger, {note}) async {},
    );

    await tester.tap(find.widgetWithText(ElevatedButton, 'Odobri'));
    await tester.pumpAndSettle();

    expect(
      find.descendant(
        of: find.byType(AlertDialog),
        matching: find.text('Odobri'),
      ),
      findsOneWidget,
    );
  });

  testWidgets('dialog title overrides the label', (tester) async {
    const action = WorkflowAction<RentalTrigger>(
      trigger: RentalTrigger.approve,
      label: 'Odobri',
      icon: Icons.check,
      confirmMessage: 'Odobriti zahtjev?',
      dialogTitle: 'Potvrda odobrenja',
    );
    await _pumpButtons(
      tester,
      actions: const [action],
      onRun: (trigger, {note}) async {},
    );

    await tester.tap(find.widgetWithText(ElevatedButton, 'Odobri'));
    await tester.pumpAndSettle();

    expect(
      find.descendant(
        of: find.byType(AlertDialog),
        matching: find.text('Potvrda odobrenja'),
      ),
      findsOneWidget,
    );
  });

  testWidgets('busy action shows a spinner and disables the other buttons', (
    tester,
  ) async {
    final completer = Completer<void>();
    await _pumpButtons(
      tester,
      actions: const [_plain, _reject],
      onRun: (trigger, {note}) => completer.future,
    );

    await tester.tap(find.widgetWithText(ElevatedButton, 'Preuzeto'));
    await tester.pump();

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    for (final button in tester.widgetList<ElevatedButton>(
      find.byType(ElevatedButton),
    )) {
      expect(button.onPressed, isNull);
    }

    completer.complete();
    await tester.pumpAndSettle();

    expect(find.byType(CircularProgressIndicator), findsNothing);
    expect(
      tester
          .widget<ElevatedButton>(
            find.widgetWithText(ElevatedButton, 'Preuzeto'),
          )
          .onPressed,
      isNotNull,
    );
  });

  testWidgets('compact renders text buttons', (tester) async {
    await _pumpButtons(
      tester,
      actions: const [_approve, _reject],
      onRun: (trigger, {note}) async {},
      compact: true,
    );

    expect(find.byType(TextButton), findsNWidgets(2));
    expect(find.byType(ElevatedButton), findsNothing);
  });
}
