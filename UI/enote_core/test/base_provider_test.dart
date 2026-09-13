import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';

class _TestProvider extends ReadOnlyProvider<Map<String, dynamic>> {
  _TestProvider({required super.apiClient, required super.endpoint});

  @override
  Map<String, dynamic> fromJson(Map<String, dynamic> json) => json;

  PagedResult<Map<String, dynamic>> testParsePage(
    http.Response response, {
    Map<String, dynamic>? params,
  }) {
    return parsePage(response, fromJson, params: params);
  }
}

void main() {
  const jsonHeaders = {'content-type': 'application/json; charset=utf-8'};

  final authState = AuthState();
  final apiClient = ApiClient(baseUrl: 'http://localhost/', authState: authState);
  final provider = _TestProvider(apiClient: apiClient, endpoint: 'test');

  group('ReadOnlyProvider.parsePage', () {
    test('malformed page field throws ApiException with parse message', () {
      final response = http.Response(
        jsonEncode({'items': [], 'page': 'x'}),
        200,
        headers: jsonHeaders,
      );

      expect(
        () => provider.testParsePage(response),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Neispravan odgovor servera.',
          ),
        ),
      );
    });

    test('non-map item in items list throws ApiException with parse message', () {
      final response = http.Response(
        jsonEncode({'items': [1]}),
        200,
        headers: jsonHeaders,
      );

      expect(
        () => provider.testParsePage(response),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Neispravan odgovor servera.',
          ),
        ),
      );
    });

    test('well-formed page parses correctly', () {
      final response = http.Response(
        jsonEncode({
          'items': [
            {'id': 1, 'name': 'Item 1'},
          ],
          'page': 1,
          'pageSize': 10,
          'totalCount': 1,
        }),
        200,
        headers: jsonHeaders,
      );

      final result = provider.testParsePage(response);
      expect(result.items, [
        {'id': 1, 'name': 'Item 1'},
      ]);
      expect(result.page, 1);
      expect(result.pageSize, 10);
      expect(result.totalCount, 1);
    });

    test('falls back to params for page and pageSize', () {
      final response = http.Response(
        jsonEncode({'items': []}),
        200,
        headers: jsonHeaders,
      );

      final result = provider.testParsePage(
        response,
        params: {'page': 3, 'pageSize': 15},
      );
      expect(result.items, isEmpty);
      expect(result.page, 3);
      expect(result.pageSize, 15);
      expect(result.totalCount, isNull);
    });
  });
}
