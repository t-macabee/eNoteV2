import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  group('LectureStatus wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in LectureStatus.values) {
        expect(LectureStatus.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/LectureStatus.cs — Scheduled, Held,
    // Cancelled (JsonStringEnumConverter, Program.cs:50).
    test('fromJson maps every backend wire value', () {
      expect(LectureStatus.fromJson('Scheduled'), LectureStatus.scheduled);
      expect(LectureStatus.fromJson('Held'), LectureStatus.held);
      expect(LectureStatus.fromJson('Cancelled'), LectureStatus.cancelled);
    });

    test('unknown names fall back to scheduled', () {
      expect(LectureStatus.fromJson('garbage'), LectureStatus.scheduled);
    });
  });

  group('AttendanceStatus wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in AttendanceStatus.values) {
        expect(AttendanceStatus.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/AttendanceStatus.cs — Pending, Present,
    // Absent.
    test('fromJson maps every backend wire value', () {
      expect(AttendanceStatus.fromJson('Pending'), AttendanceStatus.pending);
      expect(AttendanceStatus.fromJson('Present'), AttendanceStatus.present);
      expect(AttendanceStatus.fromJson('Absent'), AttendanceStatus.absent);
    });

    test('unknown names fall back to pending', () {
      expect(AttendanceStatus.fromJson('garbage'), AttendanceStatus.pending);
    });
  });

  group('InstrumentRentalStatus wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in InstrumentRentalStatus.values) {
        expect(InstrumentRentalStatus.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/InstrumentRentalStatus.cs — Pending,
    // Approved, Active, Completed, Rejected, Canceled, ReturnedEarly.
    test('fromJson maps every backend wire value', () {
      expect(
          InstrumentRentalStatus.fromJson('Pending'), InstrumentRentalStatus.pending);
      expect(InstrumentRentalStatus.fromJson('Approved'),
          InstrumentRentalStatus.approved);
      expect(
          InstrumentRentalStatus.fromJson('Active'), InstrumentRentalStatus.active);
      expect(InstrumentRentalStatus.fromJson('Completed'),
          InstrumentRentalStatus.completed);
      expect(InstrumentRentalStatus.fromJson('Rejected'),
          InstrumentRentalStatus.rejected);
      expect(InstrumentRentalStatus.fromJson('Canceled'),
          InstrumentRentalStatus.canceled);
      expect(InstrumentRentalStatus.fromJson('ReturnedEarly'),
          InstrumentRentalStatus.returnedEarly);
    });

    test('unknown names fall back to pending', () {
      expect(InstrumentRentalStatus.fromJson('garbage'),
          InstrumentRentalStatus.pending);
    });
  });

  group('EnrollmentStatus wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in EnrollmentStatus.values) {
        expect(EnrollmentStatus.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/EnrollmentStatus.cs — Active, Completed,
    // Canceled, Pending, Rejected (JsonStringEnumConverter).
    test('fromJson maps every backend wire value', () {
      expect(EnrollmentStatus.fromJson('Active'), EnrollmentStatus.active);
      expect(EnrollmentStatus.fromJson('Completed'), EnrollmentStatus.completed);
      expect(EnrollmentStatus.fromJson('Canceled'), EnrollmentStatus.canceled);
      expect(EnrollmentStatus.fromJson('Pending'), EnrollmentStatus.pending);
      expect(EnrollmentStatus.fromJson('Rejected'), EnrollmentStatus.rejected);
    });

    test('unknown names fall back to pending', () {
      expect(EnrollmentStatus.fromJson('garbage'), EnrollmentStatus.pending);
    });
  });

  group('RentalTrigger wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in RentalTrigger.values) {
        expect(RentalTrigger.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/RentalTrigger.cs — Approve, Reject,
    // Pickup, Complete, Cancel, ReturnEarly (0-based, no explicit values).
    test('fromJson maps every backend wire value', () {
      expect(RentalTrigger.fromJson(0), RentalTrigger.approve);
      expect(RentalTrigger.fromJson(1), RentalTrigger.reject);
      expect(RentalTrigger.fromJson(2), RentalTrigger.pickup);
      expect(RentalTrigger.fromJson(3), RentalTrigger.complete);
      expect(RentalTrigger.fromJson(4), RentalTrigger.cancel);
      expect(RentalTrigger.fromJson(5), RentalTrigger.returnEarly);
    });
  });

  group('AnnouncementScope wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in AnnouncementScope.values) {
        expect(AnnouncementScope.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/AnnouncementScope.cs — Course,
    // MusicStore, serialized as a string via JsonStringEnumConverter.
    test('fromJson maps every backend wire value', () {
      expect(AnnouncementScope.fromJson('Course'), AnnouncementScope.course);
      expect(
          AnnouncementScope.fromJson('MusicStore'), AnnouncementScope.musicStore);
    });

    test('unknown names fall back to course', () {
      expect(AnnouncementScope.fromJson('garbage'), AnnouncementScope.course);
    });
  });

  group('PaymentStatus wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in PaymentStatus.values) {
        expect(PaymentStatus.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/PaymentStatus.cs — RequiresAction,
    // Succeeded, Failed, Canceled, Refunded, PartiallyRefunded,
    // serialized as a string via JsonStringEnumConverter.
    test('fromJson maps every backend wire value', () {
      expect(
          PaymentStatus.fromJson('RequiresAction'), PaymentStatus.requiresAction);
      expect(PaymentStatus.fromJson('Succeeded'), PaymentStatus.succeeded);
      expect(PaymentStatus.fromJson('Failed'), PaymentStatus.failed);
      expect(PaymentStatus.fromJson('Canceled'), PaymentStatus.canceled);
      expect(PaymentStatus.fromJson('Refunded'), PaymentStatus.refunded);
      expect(PaymentStatus.fromJson('PartiallyRefunded'),
          PaymentStatus.partiallyRefunded);
    });

    test('unknown names fall back to requiresAction', () {
      expect(PaymentStatus.fromJson('garbage'), PaymentStatus.requiresAction);
    });
  });

  group('DTO tolerant enum deserialization', () {
    test(
        'LectureDto.fromJson parses string lectureStatus and preserves null attendance',
        () {
      final dto = LectureDto.fromJson({
        'lectureStatus': 'Held',
        'myAttendanceStatus': null,
      });
      expect(dto.lectureStatus, LectureStatus.held);
      expect(dto.myAttendanceStatus, isNull);
    });

    test('LectureDto.fromJson parses string attendance status', () {
      final dto = LectureDto.fromJson({
        'lectureStatus': 'Scheduled',
        'myAttendanceStatus': 'Present',
      });
      expect(dto.lectureStatus, LectureStatus.scheduled);
      expect(dto.myAttendanceStatus, AttendanceStatus.present);
    });

    test('AttendanceDto.fromJson parses string attendance status', () {
      final dto = AttendanceDto.fromJson({
        'attendanceStatus': 'Present',
      });
      expect(dto.attendanceStatus, AttendanceStatus.present);
    });

    test('InstrumentRentalDto.fromJson parses string rentalStatus', () {
      final dto = InstrumentRentalDto.fromJson({
        'rentalStatus': 'Completed',
      });
      expect(dto.rentalStatus, InstrumentRentalStatus.completed);
    });
  });
}
