import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_provider.dart';

class _RecordedRequest {
  final String method;
  final String url;
  final Map<String, dynamic>? body;

  _RecordedRequest(this.method, this.url, this.body);
}

class _ShopEmployeeMockHttpClient extends http.BaseClient {
  final List<_RecordedRequest> requests = [];
  final int statusCode;
  final String responseBody;

  _ShopEmployeeMockHttpClient({this.statusCode = 200, this.responseBody = '{}'});

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    Map<String, dynamic>? body;
    if (request is http.Request && request.body.isNotEmpty) {
      body = jsonDecode(request.body) as Map<String, dynamic>;
    }
    requests.add(_RecordedRequest(request.method, request.url.toString(), body));
    return http.StreamedResponse(
      Stream.value(utf8.encode(responseBody)),
      statusCode,
      headers: {'content-type': 'application/json'},
    );
  }
}

ShopEmployeeProvider _buildProvider(_ShopEmployeeMockHttpClient mockClient) {
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
      final mockClient = _ShopEmployeeMockHttpClient(statusCode: 204, responseBody: '');
      final provider = _buildProvider(mockClient);

      await provider.setActive(42, false);

      expect(mockClient.requests.single.method, 'PUT');
      expect(mockClient.requests.single.url, endsWith('shop/employees/42/status'));
      expect(mockClient.requests.single.body, {'isActive': false});
    });

    test('400 throws ApiException with mapped message', () async {
      final mockClient = _ShopEmployeeMockHttpClient(
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

