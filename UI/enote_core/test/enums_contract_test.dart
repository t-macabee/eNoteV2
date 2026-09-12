import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  group('LectureStatus wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in LectureStatus.values) {
        expect(LectureStatus.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/LectureStatus.cs — Scheduled=1, Held=2,
    // Cancelled=3.
    test('fromJson maps every backend wire value', () {
      expect(LectureStatus.fromJson(1), LectureStatus.scheduled);
      expect(LectureStatus.fromJson(2), LectureStatus.held);
      expect(LectureStatus.fromJson(3), LectureStatus.cancelled);
    });
  });

  group('LectureStatus.fromDynamic', () {
    test('parses every PascalCase backend wire name', () {
      expect(LectureStatus.fromDynamic('Scheduled'), LectureStatus.scheduled);
      expect(LectureStatus.fromDynamic('Held'), LectureStatus.held);
      expect(LectureStatus.fromDynamic('Cancelled'), LectureStatus.cancelled);
    });

    test('parses lowercase and mixed case strings', () {
      expect(LectureStatus.fromDynamic('scheduled'), LectureStatus.scheduled);
      expect(LectureStatus.fromDynamic('sChEdUlEd'), LectureStatus.scheduled);
      expect(LectureStatus.fromDynamic('held'), LectureStatus.held);
      expect(LectureStatus.fromDynamic('HELD'), LectureStatus.held);
      expect(LectureStatus.fromDynamic('cancelled'), LectureStatus.cancelled);
      expect(LectureStatus.fromDynamic('cAnCeLlEd'), LectureStatus.cancelled);
    });

    test('handles legacy int values', () {
      expect(LectureStatus.fromDynamic(1), LectureStatus.scheduled);
      expect(LectureStatus.fromDynamic(2), LectureStatus.held);
      expect(LectureStatus.fromDynamic(3), LectureStatus.cancelled);
    });

    test('returns default on invalid values or null', () {
      expect(LectureStatus.fromDynamic('garbage'), LectureStatus.scheduled);
      expect(LectureStatus.fromDynamic(3.5), LectureStatus.scheduled);
      expect(LectureStatus.fromDynamic(null), LectureStatus.scheduled);
      expect(LectureStatus.fromDynamic(-1), LectureStatus.scheduled);
      expect(LectureStatus.fromDynamic(99), LectureStatus.scheduled);
    });
  });

  group('AttendanceStatus wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in AttendanceStatus.values) {
        expect(AttendanceStatus.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/AttendanceStatus.cs — Pending=1,
    // Present=2, Absent=3.
    test('fromJson maps every backend wire value', () {
      expect(AttendanceStatus.fromJson(1), AttendanceStatus.pending);
      expect(AttendanceStatus.fromJson(2), AttendanceStatus.present);
      expect(AttendanceStatus.fromJson(3), AttendanceStatus.absent);
    });
  });

  group('AttendanceStatus.fromDynamic', () {
    test('parses every PascalCase backend wire name', () {
      expect(AttendanceStatus.fromDynamic('Pending'), AttendanceStatus.pending);
      expect(AttendanceStatus.fromDynamic('Present'), AttendanceStatus.present);
      expect(AttendanceStatus.fromDynamic('Absent'), AttendanceStatus.absent);
    });

    test('parses lowercase and mixed case strings', () {
      expect(AttendanceStatus.fromDynamic('pending'), AttendanceStatus.pending);
      expect(AttendanceStatus.fromDynamic('pEnDiNg'), AttendanceStatus.pending);
      expect(AttendanceStatus.fromDynamic('present'), AttendanceStatus.present);
      expect(AttendanceStatus.fromDynamic('PRESENT'), AttendanceStatus.present);
      expect(AttendanceStatus.fromDynamic('absent'), AttendanceStatus.absent);
      expect(AttendanceStatus.fromDynamic('aBsEnT'), AttendanceStatus.absent);
    });

    test('handles legacy int values', () {
      expect(AttendanceStatus.fromDynamic(1), AttendanceStatus.pending);
      expect(AttendanceStatus.fromDynamic(2), AttendanceStatus.present);
      expect(AttendanceStatus.fromDynamic(3), AttendanceStatus.absent);
    });

    test('returns default on invalid values or null', () {
      expect(AttendanceStatus.fromDynamic('garbage'), AttendanceStatus.pending);
      expect(AttendanceStatus.fromDynamic(3.5), AttendanceStatus.pending);
      expect(AttendanceStatus.fromDynamic(null), AttendanceStatus.pending);
      expect(AttendanceStatus.fromDynamic(-1), AttendanceStatus.pending);
      expect(AttendanceStatus.fromDynamic(99), AttendanceStatus.pending);
    });
  });

  group('InstrumentRentalStatus wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in InstrumentRentalStatus.values) {
        expect(InstrumentRentalStatus.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/InstrumentRentalStatus.cs — Pending=1,
    // Approved=2, Active=3, Completed=4, Rejected=5, Canceled=6,
    // ReturnedEarly=7.
    test('fromJson maps every backend wire value', () {
      expect(InstrumentRentalStatus.fromJson(1), InstrumentRentalStatus.pending);
      expect(InstrumentRentalStatus.fromJson(2), InstrumentRentalStatus.approved);
      expect(InstrumentRentalStatus.fromJson(3), InstrumentRentalStatus.active);
      expect(
          InstrumentRentalStatus.fromJson(4), InstrumentRentalStatus.completed);
      expect(InstrumentRentalStatus.fromJson(5), InstrumentRentalStatus.rejected);
      expect(InstrumentRentalStatus.fromJson(6), InstrumentRentalStatus.canceled);
      expect(
          InstrumentRentalStatus.fromJson(7), InstrumentRentalStatus.returnedEarly);
    });
  });

  group('InstrumentRentalStatus.fromDynamic', () {
    test('parses every PascalCase backend wire name', () {
      expect(InstrumentRentalStatus.fromDynamic('Pending'),
          InstrumentRentalStatus.pending);
      expect(InstrumentRentalStatus.fromDynamic('Approved'),
          InstrumentRentalStatus.approved);
      expect(InstrumentRentalStatus.fromDynamic('Active'),
          InstrumentRentalStatus.active);
      expect(InstrumentRentalStatus.fromDynamic('Completed'),
          InstrumentRentalStatus.completed);
      expect(InstrumentRentalStatus.fromDynamic('Rejected'),
          InstrumentRentalStatus.rejected);
      expect(InstrumentRentalStatus.fromDynamic('Canceled'),
          InstrumentRentalStatus.canceled);
      expect(InstrumentRentalStatus.fromDynamic('ReturnedEarly'),
          InstrumentRentalStatus.returnedEarly);
    });

    test('parses lowercase and mixed case strings', () {
      expect(InstrumentRentalStatus.fromDynamic('pending'),
          InstrumentRentalStatus.pending);
      expect(InstrumentRentalStatus.fromDynamic('approved'),
          InstrumentRentalStatus.approved);
      expect(InstrumentRentalStatus.fromDynamic('active'),
          InstrumentRentalStatus.active);
      expect(InstrumentRentalStatus.fromDynamic('completed'),
          InstrumentRentalStatus.completed);
      expect(InstrumentRentalStatus.fromDynamic('rejected'),
          InstrumentRentalStatus.rejected);
      expect(InstrumentRentalStatus.fromDynamic('canceled'),
          InstrumentRentalStatus.canceled);
      expect(InstrumentRentalStatus.fromDynamic('returnedearly'),
          InstrumentRentalStatus.returnedEarly);
      expect(InstrumentRentalStatus.fromDynamic('rEtUrNeDeArLy'),
          InstrumentRentalStatus.returnedEarly);
    });

    test('handles legacy int values', () {
      expect(InstrumentRentalStatus.fromDynamic(1),
          InstrumentRentalStatus.pending);
      expect(InstrumentRentalStatus.fromDynamic(2),
          InstrumentRentalStatus.approved);
      expect(InstrumentRentalStatus.fromDynamic(3),
          InstrumentRentalStatus.active);
      expect(InstrumentRentalStatus.fromDynamic(4),
          InstrumentRentalStatus.completed);
      expect(InstrumentRentalStatus.fromDynamic(5),
          InstrumentRentalStatus.rejected);
      expect(InstrumentRentalStatus.fromDynamic(6),
          InstrumentRentalStatus.canceled);
      expect(InstrumentRentalStatus.fromDynamic(7),
          InstrumentRentalStatus.returnedEarly);
    });

    test('returns default on invalid values or null', () {
      expect(InstrumentRentalStatus.fromDynamic('garbage'),
          InstrumentRentalStatus.pending);
      expect(InstrumentRentalStatus.fromDynamic(3.5),
          InstrumentRentalStatus.pending);
      expect(InstrumentRentalStatus.fromDynamic(null),
          InstrumentRentalStatus.pending);
      expect(InstrumentRentalStatus.fromDynamic(-1),
          InstrumentRentalStatus.pending);
      expect(InstrumentRentalStatus.fromDynamic(99),
          InstrumentRentalStatus.pending);
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

    // Contract: eNote.Domain/Enums/AnnouncementScope.cs — Course=1,
    // MusicStore=2, serialized as a string via JsonStringEnumConverter.
    test('fromJson maps every backend wire value', () {
      expect(AnnouncementScope.fromJson('Course'), AnnouncementScope.course);
      expect(
          AnnouncementScope.fromJson('MusicStore'), AnnouncementScope.musicStore);
    });
  });

  group('AnnouncementScope.fromDynamic', () {
    test('handles the string wire form', () {
      expect(AnnouncementScope.fromDynamic('Course'), AnnouncementScope.course);
      expect(AnnouncementScope.fromDynamic('MusicStore'),
          AnnouncementScope.musicStore);
    });

    // Legacy int form, still emitted by older payloads.
    test('handles the legacy int form', () {
      expect(AnnouncementScope.fromDynamic(1), AnnouncementScope.course);
      expect(AnnouncementScope.fromDynamic(2), AnnouncementScope.musicStore);
    });
  });

  group('PaymentStatus wire contract', () {
    test('every value round-trips via toJson/fromJson', () {
      for (final value in PaymentStatus.values) {
        expect(PaymentStatus.fromJson(value.toJson()), value);
      }
    });

    // Contract: eNote.Domain/Enums/PaymentStatus.cs — RequiresAction=1,
    // Succeeded=2, Failed=3, Canceled=4, Refunded=5, PartiallyRefunded=6,
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
  });

  group('PaymentStatus.fromDynamic', () {
    test('handles the string wire form', () {
      expect(PaymentStatus.fromDynamic('RequiresAction'),
          PaymentStatus.requiresAction);
      expect(PaymentStatus.fromDynamic('Succeeded'), PaymentStatus.succeeded);
      expect(PaymentStatus.fromDynamic('Failed'), PaymentStatus.failed);
      expect(PaymentStatus.fromDynamic('Canceled'), PaymentStatus.canceled);
      expect(PaymentStatus.fromDynamic('Refunded'), PaymentStatus.refunded);
      expect(PaymentStatus.fromDynamic('PartiallyRefunded'),
          PaymentStatus.partiallyRefunded);
    });

    // Legacy int form, still emitted by older payloads.
    test('handles the legacy int form', () {
      expect(PaymentStatus.fromDynamic(1), PaymentStatus.requiresAction);
      expect(PaymentStatus.fromDynamic(2), PaymentStatus.succeeded);
      expect(PaymentStatus.fromDynamic(3), PaymentStatus.failed);
      expect(PaymentStatus.fromDynamic(4), PaymentStatus.canceled);
      expect(PaymentStatus.fromDynamic(5), PaymentStatus.refunded);
      expect(PaymentStatus.fromDynamic(6), PaymentStatus.partiallyRefunded);
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
