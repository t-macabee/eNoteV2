import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/rentals/rental_timeline.dart';
import 'package:enote_mobile/theme/app_theme.dart';

const int _approvedById = 424242;
const int _rejectedById = 515151;

InstrumentRentalDto _rental({
  required InstrumentRentalStatus status,
  bool isPaid = false,
  String? requestNote = 'Trebam za kurs gitare',
  String? note,
}) {
  return InstrumentRentalDto.fromJson({
    'id': 12,
    'instrumentId': 3,
    'musicStoreId': 1,
    'studentProfileId': 1,
    'studentUserId': 3,
    'instrumentModel': 'C40',
    'instrumentType': 'Klasična gitara',
    'storeName': 'Muzika d.o.o.',
    'rentalStatus': status.toJson(),
    'requestNote': ?requestNote,
    'note': ?note,
    'requestedAt': '2026-09-01T10:12:00',
    'approvedAt': '2026-09-02T09:00:00',
    'rejectedAt': '2026-09-02T09:30:00',
    'pickedUpAt': '2026-09-03T16:30:00',
    'returnedAt': '2026-09-08T11:05:00',
    'approvedById': _approvedById,
    'rejectedById': _rejectedById,
    'fee': 40.0,
    'isProrated': false,
    'totalFee': 8.0,
    'isPaid': isPaid,
    if (isPaid) 'amountPaid': 8.0,
    if (isPaid) 'paidAt': '2026-09-09T12:00:00',
  });
}

Future<void> _pump(WidgetTester tester, InstrumentRentalDto rental) async {
  tester.view.physicalSize = const Size(400, 1200);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);
  await tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.dark,
      home: Scaffold(
        body: SingleChildScrollView(
          child: RentalTimeline(rental: rental),
        ),
      ),
    ),
  );
  await tester.pump();
}

int _filledDots(WidgetTester tester) =>
    tester.widgetList(find.byIcon(Icons.circle)).length;

int _hollowDots(WidgetTester tester) =>
    tester.widgetList(find.byIcon(Icons.circle_outlined)).length;

/// Every string the timeline actually painted, for the id-leak assertion.
List<String> _renderedText(WidgetTester tester) => tester
    .widgetList<Text>(find.byType(Text))
    .map((t) => t.data ?? '')
    .toList();

void main() {
  testWidgets('pending: requested filled, approval hollow', (tester) async {
    await _pump(tester, _rental(status: InstrumentRentalStatus.pending));

    expect(find.text('Zatraženo'), findsOneWidget);
    expect(find.text('01.09.2026. 10:12'), findsOneWidget);
    expect(find.text('Vi · „Trebam za kurs gitare"'), findsOneWidget);
    expect(find.text('Odobrenje'), findsOneWidget);
    expect(_filledDots(tester), 1);
    expect(_hollowDots(tester), 1);
  });

  testWidgets('approved: the store is the actor, pickup is hollow', (
    tester,
  ) async {
    await _pump(
      tester,
      _rental(
        status: InstrumentRentalStatus.approved,
        note: 'Preuzimanje radnim danima',
      ),
    );

    expect(find.text('Odobreno'), findsOneWidget);
    expect(find.text('02.09.2026. 09:00'), findsOneWidget);
    expect(
      find.text('Muzika d.o.o. · „Preuzimanje radnim danima"'),
      findsOneWidget,
    );
    expect(find.text('Preuzimanje'), findsOneWidget);
    expect(_filledDots(tester), 2);
    expect(_hollowDots(tester), 1);
  });

  testWidgets('active: pickup filled, return hollow', (tester) async {
    await _pump(tester, _rental(status: InstrumentRentalStatus.active));

    expect(find.text('Preuzeto'), findsOneWidget);
    expect(find.text('03.09.2026. 16:30'), findsOneWidget);
    expect(find.text('Muzika d.o.o.'), findsNWidgets(2));
    expect(find.text('Povrat'), findsOneWidget);
    expect(_filledDots(tester), 3);
    expect(_hollowDots(tester), 1);
  });

  testWidgets('completed and unpaid: payment step hollow with a dash', (
    tester,
  ) async {
    await _pump(tester, _rental(status: InstrumentRentalStatus.completed));

    expect(find.text('Vraćeno'), findsOneWidget);
    expect(find.text('08.09.2026. 11:05'), findsOneWidget);
    expect(find.text('Plaćanje'), findsOneWidget);
    expect(find.text('—'), findsOneWidget);
    expect(_filledDots(tester), 4);
    expect(_hollowDots(tester), 1);
  });

  testWidgets('completed and paid: payment step filled with paidAt', (
    tester,
  ) async {
    await _pump(
      tester,
      _rental(status: InstrumentRentalStatus.completed, isPaid: true),
    );

    expect(find.text('Plaćanje'), findsOneWidget);
    expect(find.text('09.09.2026. 12:00'), findsOneWidget);
    expect(find.text('—'), findsNothing);
    expect(_filledDots(tester), 5);
    expect(_hollowDots(tester), 0);
  });

  testWidgets('returnedEarly uses its own return label', (tester) async {
    await _pump(tester, _rental(status: InstrumentRentalStatus.returnedEarly));

    expect(find.text('Vraćeno ranije'), findsOneWidget);
    expect(find.text('Vraćeno'), findsNothing);
    expect(_filledDots(tester), 4);
    expect(_hollowDots(tester), 1);
  });

  testWidgets('rejected is terminal, red, and names the store', (tester) async {
    await _pump(
      tester,
      _rental(
        status: InstrumentRentalStatus.rejected,
        note: 'Instrument je rezervisan',
      ),
    );

    expect(find.text('Odbijeno'), findsOneWidget);
    expect(find.text('02.09.2026. 09:30'), findsOneWidget);
    expect(
      find.text('Muzika d.o.o. · „Instrument je rezervisan"'),
      findsOneWidget,
    );
    expect(find.text('Odobrenje'), findsNothing);
    expect(_filledDots(tester), 2);
    expect(_hollowDots(tester), 0);

    final label = tester.widget<Text>(find.text('Odbijeno'));
    expect(label.style?.color, AppTheme.error);
  });

  testWidgets('canceled is terminal, has no timestamp and names the student', (
    tester,
  ) async {
    await _pump(
      tester,
      _rental(
        status: InstrumentRentalStatus.canceled,
        note: 'Nije mi više potrebno',
      ),
    );

    expect(find.text('Otkazano'), findsOneWidget);
    expect(find.text('Vi · „Nije mi više potrebno"'), findsOneWidget);
    expect(find.text('—'), findsOneWidget);
    expect(_filledDots(tester), 2);
    expect(_hollowDots(tester), 0);
  });

  testWidgets('a step with no note renders the actor alone', (tester) async {
    await _pump(
      tester,
      _rental(status: InstrumentRentalStatus.pending, requestNote: null),
    );

    expect(find.text('Vi'), findsOneWidget);
  });

  testWidgets('no status ever renders approvedById or rejectedById', (
    tester,
  ) async {
    for (final status in InstrumentRentalStatus.values) {
      await _pump(tester, _rental(status: status, note: 'Napomena'));
      final rendered = _renderedText(tester).join('\n');
      expect(
        rendered.contains('$_approvedById'),
        isFalse,
        reason: 'approvedById leaked for $status',
      );
      expect(
        rendered.contains('$_rejectedById'),
        isFalse,
        reason: 'rejectedById leaked for $status',
      );
    }
  });
}
