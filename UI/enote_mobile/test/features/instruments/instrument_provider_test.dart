import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/instruments/instrument_provider.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

const _instrument = {
  'id': 3,
  'model': 'Stratocaster',
  'manufacturer': 'Fender',
  'instrumentTypeId': 1,
  'instrumentType': 'Električna gitara',
  'musicStore': 'Muzika d.o.o.',
  'isAvailable': true,
};

/// Returns a raw body (a JSON array or a failure) rather than the object
/// [RecordingHttpClient] serialises.
class _RawBodyClient extends http.BaseClient {
  final List<http.Request> requests = [];
  final int statusCode;
  final String body;
  final Object? throwOnSend;

  _RawBodyClient({this.statusCode = 200, this.body = '[]', this.throwOnSend});

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requests.add(request as http.Request);
    if (throwOnSend != null) throw throwOnSend!;
    return http.StreamedResponse(
      Stream.value(utf8.encode(body)),
      statusCode,
      headers: {'content-type': 'application/json'},
    );
  }
}

InstrumentProvider _provider(http.Client client) {
  final authState = AuthState(
    baseUrl: _baseUrl,
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return InstrumentProvider(
    apiClient: ApiClient(
      baseUrl: _baseUrl,
      authState: authState,
      httpClient: client,
    ),
  );
}

void main() {
  test('fetchPage queries instruments/public with the catalogue filters',
      () async {
    final client = RecordingHttpClient(
      body: const {'items': [_instrument], 'page': 1, 'pageSize': 20, 'totalCount': 25},
    );
    final provider = _provider(client);
    final result = await provider.fetchPage(
      2,
      InstrumentProvider.pageSize,
      'Fender',
    );

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'GET');
    expect(sent.url.path, '/api/v1/instruments/public');
    // Every key carries the `search.` prefix: an unprefixed `search` key
    // makes ASP.NET drop the rest of the query (see InstrumentProvider).
    expect(sent.url.queryParameters, {
      'search.Page': '2',
      'search.PageSize': '20',
      'search.IncludeTotalCount': 'true',
      'search.Search': 'Fender',
      'search.IsAvailable': 'true',
    });
    expect(result.items.single.manufacturer, 'Fender');
    expect(result.totalCount, 25);
  });

  test('fetchPage omits isAvailable when the toggle is off', () async {
    final client = RecordingHttpClient(body: const {'items': []});
    final provider = _provider(client);
    await provider.fetchPage(1, 20, '', onlyAvailable: false);

    final query = client.requests.single.url.queryParameters;
    expect(query.containsKey('search.IsAvailable'), isFalse);
    expect(query.containsKey('search.Search'), isFalse);
    expect(query['search.Page'], '1');
    expect(query['search.PageSize'], '20');
    expect(query['search.IncludeTotalCount'], 'true');
  });

  test('no query key is sent unprefixed', () async {
    final client = RecordingHttpClient(body: const {'items': []});
    final provider = _provider(client);
    await provider.fetchPage(1, 20, 'Fender');

    final keys = client.requests.single.url.queryParameters.keys;
    expect(keys, isNotEmpty);
    expect(keys.every((k) => k.startsWith('search.')), isTrue);
  });

  test('getById reads instruments/public/{id}', () async {
    final client = RecordingHttpClient(body: _instrument);
    final provider = _provider(client);
    final instrument = await provider.getById(3);

    expect(client.requests.single.method, 'GET');
    expect(client.requests.single.url.path, '/api/v1/instruments/public/3');
    expect(instrument.model, 'Stratocaster');
  });

  test('recommended reads student/instruments/recommended with the count',
      () async {
    final client = _RawBodyClient(
      body: jsonEncode([
        {
          'instrument': _instrument,
          'score': 0.82,
          'reasons': ['Pregledali ste slične instrumente', 'Popularno'],
        },
      ]),
    );
    final provider = _provider(client);
    final recommendations = await provider.recommended();

    expect(client.requests.single.method, 'GET');
    expect(
      client.requests.single.url.path,
      '/api/v1/student/instruments/recommended',
    );
    expect(client.requests.single.url.queryParameters, {'count': '5'});
    expect(recommendations, hasLength(1));
    expect(recommendations.single.instrument.model, 'Stratocaster');
    expect(recommendations.single.reasons, hasLength(2));
  });

  test('recommended sends the requested count', () async {
    final client = _RawBodyClient();
    final provider = _provider(client);
    await provider.recommended(count: 3);

    expect(client.requests.single.url.queryParameters, {'count': '3'});
  });

  test('recordView posts to student/instruments/{id}/view', () async {
    final client = RecordingHttpClient(statusCode: 204, body: const {});
    final provider = _provider(client);
    await provider.recordView(7);

    expect(client.requests.single.method, 'POST');
    expect(client.requests.single.url.path, '/api/v1/student/instruments/7/view');
  });

  test('recordView swallows a transport failure', () async {
    final client = _RawBodyClient(throwOnSend: const SocketFailure());
    final provider = _provider(client);

    await expectLater(provider.recordView(7), completes);
  });

  test('recordView swallows an error status', () async {
    final client = _RawBodyClient(statusCode: 500, body: '{"message":"Greška"}');
    final provider = _provider(client);

    await expectLater(provider.recordView(7), completes);
  });
}

class SocketFailure implements Exception {
  const SocketFailure();
}
