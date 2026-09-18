import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_provider.dart';

import 'helpers.dart';

ScriptedClient _mockClient({int statusCode = 200, String responseBody = '{}'}) =>
    ScriptedClient((_) => jsonResponse(responseBody, statusCode));

ShopEmployeeProvider _buildProvider(ScriptedClient mockClient) {
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(),
    httpClient: mockClient,
  );
  return ShopEmployeeProvider(apiClient: apiClient);
}

void main() {
  group('setActive', () {
    test('sends PUT with isActive body and succeeds on 204', () async {
      final mockClient = _mockClient(statusCode: 204, responseBody: '');
      final provider = _buildProvider(mockClient);

      await provider.setActive(42, false);

      expect(mockClient.recorded.single.method, 'PUT');
      expect(mockClient.recorded.single.url, endsWith('shop/employees/42/status'));
      expect(mockClient.recorded.single.body, {'isActive': false});
    });

    test('400 throws ApiException with mapped message', () async {
      final mockClient = _mockClient(
        statusCode: 400,
        responseBody: jsonEncode({'message': 'Cannot deactivate your own account.'}),
      );
      final provider = _buildProvider(mockClient);

      expect(
        () => provider.setActive(42, false),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Cannot deactivate your own account.',
          ),
        ),
      );
    });
  });
}

