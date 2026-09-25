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

  group('enrollmentStatusLabel', () {
    test('maps every value to its label', () {
      expect(enrollmentStatusLabel(EnrollmentStatus.active), 'Upisan');
      expect(enrollmentStatusLabel(EnrollmentStatus.pending), 'Na čekanju');
      expect(enrollmentStatusLabel(EnrollmentStatus.rejected), 'Odbijeno');
      expect(enrollmentStatusLabel(EnrollmentStatus.canceled), 'Otkazano');
      expect(enrollmentStatusLabel(EnrollmentStatus.completed), 'Položen');
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
