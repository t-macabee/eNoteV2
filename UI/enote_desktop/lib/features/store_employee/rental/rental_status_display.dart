import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

Color rentalStatusColor(InstrumentRentalStatus status) => switch (status) {
      InstrumentRentalStatus.pending => Colors.orange,
      InstrumentRentalStatus.approved => Colors.blue,
      InstrumentRentalStatus.active => Colors.green,
      InstrumentRentalStatus.completed => Colors.grey,
      InstrumentRentalStatus.rejected => Colors.red,
      InstrumentRentalStatus.canceled => Colors.red.shade300,
      InstrumentRentalStatus.returnedEarly => Colors.purple,
    };

String rentalTransitionSuccessMessage(RentalTrigger trigger) => switch (trigger) {
      RentalTrigger.approve => 'Zahtjev za iznajmljivanje je odobren.',
      RentalTrigger.reject => 'Zahtjev za iznajmljivanje je odbijen.',
      RentalTrigger.pickup =>
        'Preuzimanje je evidentirano. Iznajmljivanje je aktivno.',
      RentalTrigger.complete =>
        'Povrat je evidentiran. Iznajmljivanje je završeno.',
      RentalTrigger.returnEarly => 'Prijevremeni povrat je evidentiran.',
      RentalTrigger.cancel => 'Iznajmljivanje je otkazano.',
    };
