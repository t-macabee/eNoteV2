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
