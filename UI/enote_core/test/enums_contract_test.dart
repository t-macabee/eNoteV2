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
}
