import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

Color rentalStatusColor(InstrumentRentalStatus status) =>
    rentalStatusTone(status).color;

String rentalTransitionSuccessMessage(RentalTrigger trigger) =>
    switch (trigger) {
      RentalTrigger.approve => 'Zahtjev za iznajmljivanje je odobren.',
      RentalTrigger.reject => 'Zahtjev za iznajmljivanje je odbijen.',
      RentalTrigger.pickup =>
        'Preuzimanje je evidentirano. Iznajmljivanje je aktivno.',
      RentalTrigger.complete =>
        'Povrat je evidentiran. Iznajmljivanje je završeno.',
      RentalTrigger.returnEarly => 'Prijevremeni povrat je evidentiran.',
      RentalTrigger.cancel => 'Iznajmljivanje je otkazano.',
    };
