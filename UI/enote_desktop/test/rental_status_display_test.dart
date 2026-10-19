import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_status_display.dart';

void main() {
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
