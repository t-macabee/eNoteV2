import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'package:enote_mobile/features/instruments/instrument_labels.dart';

void main() {
  test('availability chip uses success and neutral', () {
    final available = instrumentAvailabilityChip(true);
    expect(available.label, 'Dostupno');
    expect(available.tone, StatusTone.success);

    final unavailable = instrumentAvailabilityChip(false);
    expect(unavailable.label, 'Nedostupno');
    expect(unavailable.tone, StatusTone.neutral);
  });
}
