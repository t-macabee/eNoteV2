import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  group('rentalStatusLabel', () {
    test('every value has a non-empty label', () {
      for (final value in InstrumentRentalStatus.values) {
        expect(rentalStatusLabel(value).trim(), isNotEmpty);
      }
    });
  });

  group('lectureStatusLabel', () {
    test('every value has a non-empty label', () {
      for (final value in LectureStatus.values) {
        expect(lectureStatusLabel(value).trim(), isNotEmpty);
      }
    });
  });

  group('lectureTypeLabel', () {
    test('every value has a non-empty label', () {
      for (final value in LectureType.values) {
        expect(lectureTypeLabel(value).trim(), isNotEmpty);
      }
    });
  });
}
