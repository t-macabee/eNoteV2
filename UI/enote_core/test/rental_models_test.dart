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
}
