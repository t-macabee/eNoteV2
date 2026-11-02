import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  test('rentalStatusTone maps every rental status', () {
    const expected = {
      InstrumentRentalStatus.pending: StatusTone.warning,
      InstrumentRentalStatus.approved: StatusTone.info,
      InstrumentRentalStatus.active: StatusTone.success,
      InstrumentRentalStatus.completed: StatusTone.neutral,
      InstrumentRentalStatus.rejected: StatusTone.danger,
      InstrumentRentalStatus.canceled: StatusTone.neutral,
      InstrumentRentalStatus.returnedEarly: StatusTone.accent,
    };
    for (final status in InstrumentRentalStatus.values) {
      expect(rentalStatusTone(status), expected[status], reason: '$status');
    }
  });

  test('enrollmentStatusTone maps every enrollment status', () {
    const expected = {
      EnrollmentStatus.pending: StatusTone.warning,
      EnrollmentStatus.active: StatusTone.success,
      EnrollmentStatus.completed: StatusTone.info,
      EnrollmentStatus.rejected: StatusTone.danger,
      EnrollmentStatus.canceled: StatusTone.neutral,
    };
    for (final status in EnrollmentStatus.values) {
      expect(enrollmentStatusTone(status), expected[status], reason: '$status');
    }
  });

  test('paymentStatusTone maps every payment status', () {
    const expected = {
      PaymentStatus.requiresAction: StatusTone.warning,
      PaymentStatus.succeeded: StatusTone.success,
      PaymentStatus.failed: StatusTone.danger,
      PaymentStatus.canceled: StatusTone.neutral,
      PaymentStatus.refunded: StatusTone.accent,
      PaymentStatus.partiallyRefunded: StatusTone.accent,
    };
    for (final status in PaymentStatus.values) {
      expect(paymentStatusTone(status), expected[status], reason: '$status');
    }
  });

  test('lectureStatusTone maps every lecture status', () {
    const expected = {
      LectureStatus.scheduled: StatusTone.info,
      LectureStatus.held: StatusTone.success,
      LectureStatus.cancelled: StatusTone.danger,
    };
    for (final status in LectureStatus.values) {
      expect(lectureStatusTone(status), expected[status], reason: '$status');
    }
  });

  test('attendanceStatusLabel and attendanceStatusTone map every status', () {
    const expectedLabels = {
      AttendanceStatus.pending: 'Na čekanju',
      AttendanceStatus.present: 'Prisutan',
      AttendanceStatus.absent: 'Odsutan',
    };
    const expectedTones = {
      AttendanceStatus.pending: StatusTone.warning,
      AttendanceStatus.present: StatusTone.success,
      AttendanceStatus.absent: StatusTone.danger,
    };
    for (final status in AttendanceStatus.values) {
      expect(
        attendanceStatusLabel(status),
        expectedLabels[status],
        reason: '$status',
      );
      expect(
        attendanceStatusTone(status),
        expectedTones[status],
        reason: '$status',
      );
    }
  });

  test(
    'paymentStatusLabel gives a non-empty label for every payment status',
    () {
      for (final status in PaymentStatus.values) {
        expect(paymentStatusLabel(status), isNotEmpty, reason: '$status');
      }
    },
  );
}
