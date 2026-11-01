import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_status_display.dart';
import 'package:enote_desktop/theme/app_theme.dart';

void main() {
  test('rentalStatusColor maps active rentals to the success color', () {
    expect(rentalStatusColor(InstrumentRentalStatus.active), AppTheme.success);
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
