import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_provider.dart';

import 'helpers.dart';

ScriptedClient _mockClient({required String responseBody}) =>
    ScriptedClient((_) => jsonResponse(responseBody, 200));

ShopStoreProvider _buildProvider(ScriptedClient mockClient) {
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(),
    httpClient: mockClient,
  );
  return ShopStoreProvider(apiClient: apiClient);
}

void main() {
  group('ShopStoreProvider', () {
    test('getOwnStore sends GET to shop/store and returns MusicStoreDto', () async {
      final storeJson = jsonEncode({
        'id': 1,
        'storeName': 'My Store',
        'businessHours': '08:00 - 16:00',
        'phoneNumber': '+387 61 111 222',
        'addressId': 2,
        'addressStreet': 'Main Street 1',
        'addressCity': 'Sarajevo',
      });

      final mockClient = _mockClient(responseBody: storeJson);
      final provider = _buildProvider(mockClient);

      final store = await provider.getOwnStore();

      expect(mockClient.recorded.single.method, 'GET');
      expect(mockClient.recorded.single.url, endsWith('shop/store'));
      expect(store.id, 1);
      expect(store.storeName, 'My Store');
      expect(store.businessHours, '08:00 - 16:00');
    });

    test('updateOwnStore sends PUT to shop/store and returns updated MusicStoreDto', () async {
      final storeJson = jsonEncode({
        'id': 1,
        'storeName': 'My Updated Store',
        'businessHours': '09:00 - 17:00',
        'phoneNumber': '+387 61 333 444',
      });

      final mockClient = _mockClient(responseBody: storeJson);
      final provider = _buildProvider(mockClient);

      final updated = await provider.updateOwnStore({
        'storeName': 'My Updated Store',
        'businessHours': '09:00 - 17:00',
        'phoneNumber': '+387 61 333 444',
      });

      expect(mockClient.recorded.single.method, 'PUT');
      expect(mockClient.recorded.single.url, endsWith('shop/store'));
      expect(mockClient.recorded.single.body?['storeName'], 'My Updated Store');
      expect(updated.storeName, 'My Updated Store');
    });
  });
}
