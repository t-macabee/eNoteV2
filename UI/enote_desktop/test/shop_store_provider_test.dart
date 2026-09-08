import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_provider.dart';

class _RecordedRequest {
  final String method;
  final String url;
  final Map<String, dynamic>? body;

  _RecordedRequest(this.method, this.url, this.body);
}

class _ShopStoreMockHttpClient extends http.BaseClient {
  final List<_RecordedRequest> requests = [];
  final String responseBody;

  _ShopStoreMockHttpClient({required this.responseBody});

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    Map<String, dynamic>? body;
    if (request is http.Request && request.body.isNotEmpty) {
      body = jsonDecode(request.body) as Map<String, dynamic>;
    }
    requests.add(_RecordedRequest(request.method, request.url.toString(), body));
    return http.StreamedResponse(
      Stream.value(utf8.encode(responseBody)),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

ShopStoreProvider _buildProvider(_ShopStoreMockHttpClient mockClient) {
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

      final mockClient = _ShopStoreMockHttpClient(responseBody: storeJson);
      final provider = _buildProvider(mockClient);

      final store = await provider.getOwnStore();

      expect(mockClient.requests.single.method, 'GET');
      expect(mockClient.requests.single.url, endsWith('shop/store'));
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

      final mockClient = _ShopStoreMockHttpClient(responseBody: storeJson);
      final provider = _buildProvider(mockClient);

      final updated = await provider.updateOwnStore({
        'storeName': 'My Updated Store',
        'businessHours': '09:00 - 17:00',
        'phoneNumber': '+387 61 333 444',
      });

      expect(mockClient.requests.single.method, 'PUT');
      expect(mockClient.requests.single.url, endsWith('shop/store'));
      expect(mockClient.requests.single.body?['storeName'], 'My Updated Store');
      expect(updated.storeName, 'My Updated Store');
    });
  });
}
