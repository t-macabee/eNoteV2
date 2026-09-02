import 'package:flutter_test/flutter_test.dart';
import 'package:enote_core/enote_core.dart';

void main() {
  group('MusicStoreDto', () {
    test('fromJson and toJson round-trip with all fields', () {
      final json = {
        'id': 42,
        'storeName': 'Sarajevo Music Shop',
        'businessHours': '08:00 - 20:00',
        'phoneNumber': '+387 33 123 456',
        'imagePath': '/uploads/stores/42.jpg',
        'addressId': 5,
        'addressStreet': 'Ferhadija 12',
        'addressCity': 'Sarajevo',
      };

      final dto = MusicStoreDto.fromJson(json);

      expect(dto.id, 42);
      expect(dto.storeName, 'Sarajevo Music Shop');
      expect(dto.businessHours, '08:00 - 20:00');
      expect(dto.phoneNumber, '+387 33 123 456');
      expect(dto.imagePath, '/uploads/stores/42.jpg');
      expect(dto.addressId, 5);
      expect(dto.addressStreet, 'Ferhadija 12');
      expect(dto.addressCity, 'Sarajevo');

      final serialized = dto.toJson();
      expect(serialized, equals(json));
    });

    test('fromJson handles null optional fields', () {
      final json = {
        'id': 1,
        'storeName': 'Minimal Store',
        'businessHours': '09:00 - 17:00',
      };

      final dto = MusicStoreDto.fromJson(json);

      expect(dto.id, 1);
      expect(dto.storeName, 'Minimal Store');
      expect(dto.phoneNumber, isNull);
      expect(dto.imagePath, isNull);
      expect(dto.addressId, isNull);
      expect(dto.addressStreet, isNull);
      expect(dto.addressCity, isNull);

      final serialized = dto.toJson();
      expect(serialized['phoneNumber'], isNull);
      expect(serialized['imagePath'], isNull);
      expect(serialized['addressId'], isNull);
    });
  });

  group('MusicStoreRequest', () {
    test('toJson serializes required and optional fields', () {
      final request = MusicStoreRequest(
        storeName: 'Test Store',
        businessHours: '09:00 - 18:00',
        phoneNumber: '+387 61 000 000',
        addressId: 10,
      );

      final json = request.toJson();

      expect(json['storeName'], 'Test Store');
      expect(json['businessHours'], '09:00 - 18:00');
      expect(json['phoneNumber'], '+387 61 000 000');
      expect(json['addressId'], 10);
    });

    test('toJson omits null optional fields', () {
      final request = MusicStoreRequest(
        storeName: 'Test Store',
        businessHours: '09:00 - 18:00',
      );

      final json = request.toJson();

      expect(json['storeName'], 'Test Store');
      expect(json['businessHours'], '09:00 - 18:00');
      expect(json.containsKey('phoneNumber'), isFalse);
      expect(json.containsKey('addressId'), isFalse);
    });
  });
}
