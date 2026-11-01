import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_status_display.dart';

void main() {
  group('rentalTransitionActions', () {
    final cases =
        <
          String,
          ({
            InstrumentRentalStatus status,
            DateTime? dueAt,
            List<String> labels,
          })
        >{
          'pending': (
            status: InstrumentRentalStatus.pending,
            dueAt: null,
            labels: ['Odobri', 'Odbij'],
          ),
          'approved': (
            status: InstrumentRentalStatus.approved,
            dueAt: null,
            labels: ['Preuzeto', 'Otkaži'],
          ),
          'active before due': (
            status: InstrumentRentalStatus.active,
            dueAt: DateTime.now().add(const Duration(days: 2)),
            labels: ['Prijevremeni povrat'],
          ),
          'active after due': (
            status: InstrumentRentalStatus.active,
            dueAt: DateTime.now().subtract(const Duration(days: 1)),
            labels: ['Završi'],
          ),
          'active without a due date': (
            status: InstrumentRentalStatus.active,
            dueAt: null,
            labels: ['Prijevremeni povrat'],
          ),
          'completed': (
            status: InstrumentRentalStatus.completed,
            dueAt: null,
            labels: <String>[],
          ),
          'rejected': (
            status: InstrumentRentalStatus.rejected,
            dueAt: null,
            labels: <String>[],
          ),
          'canceled': (
            status: InstrumentRentalStatus.canceled,
            dueAt: null,
            labels: <String>[],
          ),
          'returnedEarly': (
            status: InstrumentRentalStatus.returnedEarly,
            dueAt: null,
            labels: <String>[],
          ),
        };

    for (final entry in cases.entries) {
      test('${entry.key} offers ${entry.value.labels}', () {
        final actions = rentalTransitionActions(
          entry.value.status,
          dueAt: entry.value.dueAt,
        );

        expect(
          actions.map((action) => action.label).toList(),
          entry.value.labels,
        );
      });
    }

    test('reject and cancel require a note and use the danger tone', () {
      final reject = rentalTransitionActions(InstrumentRentalStatus.pending)
          .singleWhere((action) => action.trigger == RentalTrigger.reject);
      final cancel = rentalTransitionActions(InstrumentRentalStatus.approved)
          .singleWhere((action) => action.trigger == RentalTrigger.cancel);

      for (final action in [reject, cancel]) {
        expect(action.requiresNote, isTrue);
        expect(action.tone, StatusTone.danger);
        expect(action.confirmMessage, isNull);
      }
    });
  });

  test('rentalTransitionSuccessMessage describes every trigger', () {
    expect(
      rentalTransitionSuccessMessage(RentalTrigger.approve),
      'Zahtjev za iznajmljivanje je odobren.',
    );
    expect(
      rentalTransitionSuccessMessage(RentalTrigger.reject),
      'Zahtjev za iznajmljivanje je odbijen.',
    );
    expect(
      rentalTransitionSuccessMessage(RentalTrigger.pickup),
      'Preuzimanje je evidentirano. Iznajmljivanje je aktivno.',
    );
    expect(
      rentalTransitionSuccessMessage(RentalTrigger.complete),
      'Povrat je evidentiran. Iznajmljivanje je završeno.',
    );
    expect(
      rentalTransitionSuccessMessage(RentalTrigger.returnEarly),
      'Prijevremeni povrat je evidentiran.',
    );
    expect(
      rentalTransitionSuccessMessage(RentalTrigger.cancel),
      'Iznajmljivanje je otkazano.',
    );
  });
}
