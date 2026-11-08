import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  group('CourseEnrollmentDto', () {
    test('parses the cancellation time and reason', () {
      final dto = CourseEnrollmentDto.fromJson({
        'id': 11,
        'studentId': 3,
        'studentName': 'Ana',
        'courseId': 7,
        'courseName': 'Gitara',
        'enrollmentStatus': 'Canceled',
        'canceledAt': '2026-09-21T10:00:00',
        'cancellationReason': 'Kurs se ukida.',
      });

      expect(dto.canceledAt, DateTime.parse('2026-09-21T10:00:00'));
      expect(dto.cancellationReason, 'Kurs se ukida.');
    });

    test('statusReason is the cancellation reason for a canceled enrollment', () {
      final dto = CourseEnrollmentDto.fromJson({
        'id': 11,
        'studentId': 3,
        'studentName': 'Ana',
        'courseId': 7,
        'courseName': 'Gitara',
        'enrollmentStatus': 'Canceled',
        'decisionNote': 'Ranije odobreno.',
        'cancellationReason': 'Kurs se ukida.',
      });

      expect(dto.statusReason, 'Kurs se ukida.');
    });

    test('statusReason is the decision note for a rejected enrollment', () {
      final dto = CourseEnrollmentDto.fromJson({
        'id': 11,
        'studentId': 3,
        'studentName': 'Ana',
        'courseId': 7,
        'courseName': 'Gitara',
        'enrollmentStatus': 'Rejected',
        'decisionNote': 'Popunjeno.',
        'cancellationReason': 'Kurs se ukida.',
      });

      expect(dto.statusReason, 'Popunjeno.');
    });
  });

  group('InstrumentRentalDto', () {
    test('parses the decision note and the cancellation time', () {
      final dto = InstrumentRentalDto.fromJson({
        'id': 5,
        'instrumentId': 2,
        'musicStoreId': 1,
        'studentProfileId': 4,
        'studentUserId': 9,
        'rentalStatus': 'Rejected',
        'requestedAt': '2026-09-01T08:00:00',
        'decisionNote': 'Instrument je na servisu.',
        'canceledAt': '2026-09-02T09:30:00',
        'isPaid': false,
      });

      expect(dto.decisionNote, 'Instrument je na servisu.');
      expect(dto.canceledAt, DateTime.parse('2026-09-02T09:30:00'));
    });
  });
}
