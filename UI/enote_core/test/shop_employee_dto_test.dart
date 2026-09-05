import 'package:flutter_test/flutter_test.dart';
import 'package:enote_core/enote_core.dart';

void main() {
  group('ShopEmployeeDto', () {
    test('fromJson and toJson round-trip with all fields including storeName', () {
      final json = {
        'id': 10,
        'appUserId': 101,
        'musicStoreId': 3,
        'storeName': 'Sarajevo Guitar Shop',
        'firstName': 'Edin',
        'lastName': 'Dzeko',
        'username': 'edzeko',
        'isManager': true,
        'isActive': true,
      };

      final dto = ShopEmployeeDto.fromJson(json);

      expect(dto.id, 10);
      expect(dto.appUserId, 101);
      expect(dto.musicStoreId, 3);
      expect(dto.storeName, 'Sarajevo Guitar Shop');
      expect(dto.musicStoreName, 'Sarajevo Guitar Shop');
      expect(dto.firstName, 'Edin');
      expect(dto.lastName, 'Dzeko');
      expect(dto.username, 'edzeko');
      expect(dto.isManager, isTrue);
      expect(dto.isActive, isTrue);

      final serialized = dto.toJson();
      expect(serialized, equals(json));
    });

    test('fromJson falls back to musicStoreName if storeName is not present', () {
      final json = {
        'id': 11,
        'appUserId': 102,
        'musicStoreId': 4,
        'musicStoreName': 'Mostar Piano Store',
        'isManager': false,
        'isActive': true,
      };

      final dto = ShopEmployeeDto.fromJson(json);

      expect(dto.storeName, 'Mostar Piano Store');
      expect(dto.musicStoreName, 'Mostar Piano Store');
    });
  });
}
