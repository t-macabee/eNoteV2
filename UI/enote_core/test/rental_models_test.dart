import 'package:flutter_test/flutter_test.dart';
import 'package:enote_core/enote_core.dart';

void main() {
  group('InstrumentRecommendationDto', () {
    test('fromJson round-trips a full payload', () {
      final json = {
        'instrument': {
          'id': 1,
          'model': 'Stratocaster',
          'manufacturer': 'Fender',
          'instrumentTypeId': 2,
          'instrumentType': 'Gitara',
          'musicStore': 'Muzika d.o.o.',
          'isAvailable': true,
        },
        'score': 0.95,
        'reasons': ['Slično vašim pregledima', 'Popularno'],
      };
      final dto =
          InstrumentRecommendationDto.fromJson(Map<String, dynamic>.from(json));
      expect(dto.instrument.model, 'Stratocaster');
      expect(dto.score, 0.95);
      expect(dto.reasons, ['Slično vašim pregledima', 'Popularno']);
      final roundTrip = InstrumentRecommendationDto.fromJson(dto.toJson());
      expect(roundTrip.score, dto.score);
      expect(roundTrip.reasons, dto.reasons);
      expect(roundTrip.instrument.id, dto.instrument.id);
    });

    test('fromJson defaults missing score and reasons', () {
      final dto = InstrumentRecommendationDto.fromJson({
        'instrument': {'id': 2, 'model': 'C40', 'manufacturer': 'Yamaha'},
      });
      expect(dto.score, 0.0);
      expect(dto.reasons, isEmpty);
    });
  });

  group('CreatePaymentIntentResponse', () {
    test('missing currency falls back to an empty string', () {
      final dto = CreatePaymentIntentResponse.fromJson({
        'rentalId': 1,
        'paymentIntentId': 'pi_1',
        'clientSecret': 'pi_1_secret',
        'amountCents': 100,
        'status': 'RequiresAction',
      });
      expect(dto.currency, '');
    });
  });

  group('RentalDebtDto', () {
    test('fromJson with rentalId present and absent', () {
      final withId = RentalDebtDto.fromJson(
          {'hasUnpaidDebt': true, 'rentalId': 7});
      expect(withId.hasUnpaidDebt, isTrue);
      expect(withId.rentalId, 7);

      final withoutId =
          RentalDebtDto.fromJson({'hasUnpaidDebt': false});
      expect(withoutId.hasUnpaidDebt, isFalse);
      expect(withoutId.rentalId, isNull);
    });
  });

  group('InstrumentRentalDto.instrumentIsActive', () {
    InstrumentRentalDto rental(Map<String, dynamic> extra) =>
        InstrumentRentalDto.fromJson({
          'id': 1,
          'instrumentModel': 'C40',
          'instrumentType': 'Gitara',
          'storeName': 'Muzika d.o.o.',
          'rentalStatus': 'Completed',
          'requestedAt': '2026-06-01T00:00:00Z',
          'fee': 40.0,
          ...extra,
        });

    test('reads the archived flag from the payload', () {
      expect(rental({'instrumentIsActive': false}).instrumentIsActive, isFalse);
      expect(rental({'instrumentIsActive': true}).instrumentIsActive, isTrue);
    });

    test('treats a payload without the flag as an active instrument', () {
      expect(rental({}).instrumentIsActive, isTrue);
    });
  });

  group('InstrumentRentalDto.pickupDeadline', () {
    InstrumentRentalDto rental(Map<String, dynamic> extra) =>
        InstrumentRentalDto.fromJson({
          'id': 1,
          'instrumentModel': 'C40',
          'instrumentType': 'Gitara',
          'storeName': 'Muzika d.o.o.',
          'rentalStatus': 'Approved',
          'requestedAt': '2026-06-01T00:00:00Z',
          'fee': 40.0,
          ...extra,
        });

    test('reads the pickup deadline from the payload', () {
      final dto = rental({'pickupDeadline': '2026-06-17T12:00:00Z'});

      expect(dto.pickupDeadline, isNotNull);
      expect(dto.pickupDeadline!.toUtc(), DateTime.utc(2026, 6, 17, 12));
    });

    test('treats a payload without the deadline as null', () {
      expect(rental({}).pickupDeadline, isNull);
    });

    test('writes the deadline only when it is set', () {
      final deadline = DateTime.utc(2026, 6, 17, 12);
      final json =
          rental({'pickupDeadline': deadline.toIso8601String()}).toJson();

      expect(json.containsKey('pickupDeadline'), isTrue);
      expect(DateTime.parse(json['pickupDeadline'] as String).toUtc(), deadline);
      expect(rental({}).toJson().containsKey('pickupDeadline'), isFalse);
    });
  });

  group('InstrumentRentalDto.chargeLabel', () {
    InstrumentRentalDto rental(Map<String, dynamic> extra) =>
        InstrumentRentalDto.fromJson({
          'id': 1,
          'instrumentModel': 'C40',
          'instrumentType': 'Gitara',
          'storeName': 'Muzika d.o.o.',
          'rentalStatus': 'Completed',
          'requestedAt': '2026-06-01T00:00:00Z',
          'fee': 40.0,
          ...extra,
        });

    test('shows days times the daily fee', () {
      expect(
        rental({'daysCharged': 6, 'dailyFee': 1.33}).chargeLabel,
        '6 dana × 1.33 KM',
      );
    });

    test('shows a dash before anything is charged', () {
      expect(rental({}).chargeLabel, '—');
    });
  });

  group('InstrumentRentalDto.term and penalty', () {
    InstrumentRentalDto rental(Map<String, dynamic> extra) =>
        InstrumentRentalDto.fromJson({
          'id': 1,
          'instrumentModel': 'C40',
          'instrumentType': 'Gitara',
          'storeName': 'Muzika d.o.o.',
          'rentalStatus': 'Active',
          'requestedAt': '2026-03-01T10:00:00Z',
          'fee': 90.0,
          ...extra,
        });

    test('reads and writes the term and penalty fields', () {
      final dto = rental({
        'dueAt': '2026-03-31T10:00:00Z',
        'renewalOpensAt': '2026-03-26T10:00:00Z',
        'dailyPenaltyFee': 1.5,
        'overdueDays': 4,
        'penaltyFee': 6.0,
      });

      expect(dto.dueAt!.toUtc(), DateTime.utc(2026, 3, 31, 10));
      expect(dto.renewalOpensAt!.toUtc(), DateTime.utc(2026, 3, 26, 10));
      expect(dto.dailyPenaltyFee, 1.5);
      expect(dto.overdueDays, 4);
      expect(dto.penaltyFee, 6.0);
      expect(dto.isOverdue, isTrue);

      final json = dto.toJson();
      expect(DateTime.parse(json['dueAt'] as String).toUtc(), DateTime.utc(2026, 3, 31, 10));
      expect(DateTime.parse(json['renewalOpensAt'] as String).toUtc(), DateTime.utc(2026, 3, 26, 10));
      expect(json['dailyPenaltyFee'], 1.5);
      expect(json['overdueDays'], 4);
      expect(json['penaltyFee'], 6.0);
    });

    test('defaults the daily penalty fee and treats a missing overdue day as zero', () {
      final dto = rental({});

      expect(dto.dailyPenaltyFee, 0.0);
      expect(dto.overdueDays, isNull);
      expect(dto.isOverdue, isFalse);
      expect(dto.toJson()['dailyPenaltyFee'], 0.0);
    });

    test('isOverdue is false for zero overdue days', () {
      expect(rental({'overdueDays': 0}).isOverdue, isFalse);
    });

    test('hasOverdueDays is true for any positive overdue day count', () {
      expect(rental({'overdueDays': 2}).hasOverdueDays, isTrue);
      expect(rental({'overdueDays': 0}).hasOverdueDays, isFalse);
      expect(rental({}).hasOverdueDays, isFalse);
    });

    test('isOverdue is true only while the rental is active', () {
      expect(rental({'overdueDays': 4}).isOverdue, isTrue);
      expect(
        rental({'rentalStatus': 'Completed', 'overdueDays': 2}).isOverdue,
        isFalse,
      );
      expect(rental({'overdueDays': 0}).isOverdue, isFalse);
    });

    test('hasPenaltyTerms covers every status that carries penalty terms', () {
      for (final status in [
        'Approved',
        'Active',
        'Completed',
        'ReturnedEarly',
      ]) {
        expect(
          rental({'rentalStatus': status}).hasPenaltyTerms,
          isTrue,
          reason: status,
        );
      }

      for (final status in ['Pending', 'Rejected', 'Canceled']) {
        expect(
          rental({'rentalStatus': status}).hasPenaltyTerms,
          isFalse,
          reason: status,
        );
      }
    });

    test('isRenewableAt uses the half-open renewal window', () {
      final dto = rental({
        'dueAt': '2026-03-31T10:00:00Z',
        'renewalOpensAt': '2026-03-26T10:00:00Z',
      });

      expect(dto.isRenewableAt(DateTime.utc(2026, 3, 26, 10)), isTrue);
      expect(dto.isRenewableAt(DateTime.utc(2026, 3, 26, 9, 59)), isFalse);
      expect(dto.isRenewableAt(DateTime.utc(2026, 3, 31, 10)), isFalse);
      expect(
        rental({
          'rentalStatus': 'Completed',
          'dueAt': '2026-03-31T10:00:00Z',
          'renewalOpensAt': '2026-03-26T10:00:00Z',
        }).isRenewableAt(DateTime.utc(2026, 3, 27, 10)),
        isFalse,
      );
    });
  });
}
