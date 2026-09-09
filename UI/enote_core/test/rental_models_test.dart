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
}
