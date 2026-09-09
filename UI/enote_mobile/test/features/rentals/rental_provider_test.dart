import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

const _rental = {
  'id': 12,
  'instrumentId': 3,
  'musicStoreId': 1,
  'studentProfileId': 1,
  'studentUserId': 3,
  'instrumentModel': 'Stratocaster',
  'instrumentType': 'Električna gitara',
  'storeName': 'Muzika d.o.o.',
  'rentalStatus': 1,
  'requestedAt': '2026-09-01T10:12:00',
  'fee': 40.0,
  'isProrated': false,
  'isPaid': false,
};

RentalProvider _provider(http.Client client) {
  final authState = AuthState(
    baseUrl: _baseUrl,
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return RentalProvider(
    apiClient: ApiClient(
      baseUrl: _baseUrl,
      authState: authState,
      httpClient: client,
    ),
  );
}

void main() {
  test('fetchPage queries student/rentals with paging only by default',
      () async {
    final client = RecordingHttpClient(
      body: const {'items': [_rental], 'page': 1, 'pageSize': 20, 'totalCount': 1},
    );
    final provider = _provider(client);
    final result = await provider.fetchPage(1, RentalProvider.pageSize);

    expect(client.requests.single.method, 'GET');
    expect(client.requests.single.url.path, '/api/v1/student/rentals');
    expect(client.requests.single.url.queryParameters, {
      'page': '1',
      'pageSize': '20',
      'includeTotalCount': 'true',
    });
    expect(result.items.single.rentalStatus, InstrumentRentalStatus.pending);
  });

  test('fetchPage sends rentalStatus as the server int and the instrument id',
      () async {
    final client = RecordingHttpClient(body: const {'items': []});
    final provider = _provider(client);
    await provider.fetchPage(
      3,
      20,
      rentalStatus: InstrumentRentalStatus.completed,
      instrumentId: 3,
    );

    expect(client.requests.single.url.queryParameters, {
      'page': '3',
      'pageSize': '20',
      'includeTotalCount': 'true',
      'rentalStatus': '4',
      'instrumentId': '3',
    });
  });

  test('every status maps to its 1..7 server value', () async {
    const expected = {
      InstrumentRentalStatus.pending: '1',
      InstrumentRentalStatus.approved: '2',
      InstrumentRentalStatus.active: '3',
      InstrumentRentalStatus.completed: '4',
      InstrumentRentalStatus.rejected: '5',
      InstrumentRentalStatus.canceled: '6',
      InstrumentRentalStatus.returnedEarly: '7',
    };
    for (final entry in expected.entries) {
      final client = RecordingHttpClient(body: const {'items': []});
      final provider = _provider(client);
      await provider.fetchPage(1, 20, rentalStatus: entry.key);
      expect(
        client.requests.single.url.queryParameters['rentalStatus'],
        entry.value,
        reason: '${entry.key} must map to ${entry.value}',
      );
    }
  });

  test('a string rentalStatus from the server is parsed, not defaulted',
      () async {
    // The API serialises the enum as its name; core's DTO only understands
    // the int, so the provider normalises before decoding.
    final client = RecordingHttpClient(
      body: {..._rental, 'rentalStatus': 'Completed'},
    );
    final provider = _provider(client);
    final rental = await provider.getById(1);

    expect(rental.rentalStatus, InstrumentRentalStatus.completed);
  });

  test('every status name round-trips through the normaliser', () {
    for (final status in InstrumentRentalStatus.values) {
      final name = '${status.name[0].toUpperCase()}${status.name.substring(1)}';
      final normalized = RentalProvider.normalizeRentalStatus({
        'rentalStatus': name,
      });
      expect(
        normalized['rentalStatus'],
        status.toJson(),
        reason: '$name must normalise to ${status.toJson()}',
      );
    }
  });

  test('an int rentalStatus is left untouched', () {
    final json = {'rentalStatus': 4};
    expect(RentalProvider.normalizeRentalStatus(json)['rentalStatus'], 4);
  });

  test('getById reads student/rentals/{id}', () async {
    final client = RecordingHttpClient(body: _rental);
    final provider = _provider(client);
    final rental = await provider.getById(12);

    expect(client.requests.single.url.path, '/api/v1/student/rentals/12');
    expect(rental.instrumentModel, 'Stratocaster');
  });

  test('createRequest posts the instrument id and the optional note',
      () async {
    final client = RecordingHttpClient(statusCode: 201, body: _rental);
    final provider = _provider(client);
    final created = await provider.createRequest(
      RentalCreateRequest(instrumentId: 3, note: 'Trebam za kurs gitare'),
    );

    final sent = client.requests.single;
    expect(sent.method, 'POST');
    expect(sent.url.path, '/api/v1/student/rentals');
    expect(jsonDecode(sent.body), {
      'instrumentId': 3,
      'note': 'Trebam za kurs gitare',
    });
    expect(created.id, 12);
  });

  test('createRequest omits a null note', () async {
    final client = RecordingHttpClient(statusCode: 201, body: _rental);
    final provider = _provider(client);
    await provider.createRequest(RentalCreateRequest(instrumentId: 3));

    expect(jsonDecode(client.requests.single.body), {'instrumentId': 3});
  });

  test('cancel posts to student/rentals/{id}/cancel with the note', () async {
    final client = RecordingHttpClient(statusCode: 204, body: const {});
    final provider = _provider(client);
    await provider.cancel(12, note: 'Nije mi više potrebno');

    final sent = client.requests.single;
    expect(sent.method, 'POST');
    expect(sent.url.path, '/api/v1/student/rentals/12/cancel');
    expect(jsonDecode(sent.body), {'note': 'Nije mi više potrebno'});
  });

  test('cancel sends an empty body when no note is given', () async {
    final client = RecordingHttpClient(statusCode: 204, body: const {});
    final provider = _provider(client);
    await provider.cancel(12);

    expect(jsonDecode(client.requests.single.body), isEmpty);
  });

  test('debt reads student/rentals/debt and decodes the rental id', () async {
    final client = RecordingHttpClient(
      body: const {'hasUnpaidDebt': true, 'rentalId': 9},
    );
    final provider = _provider(client);
    final debt = await provider.debt();

    expect(client.requests.single.method, 'GET');
    expect(client.requests.single.url.path, '/api/v1/student/rentals/debt');
    expect(debt.hasUnpaidDebt, isTrue);
    expect(debt.rentalId, 9);
  });

  test('debt decodes the no-debt shape', () async {
    final client = RecordingHttpClient(body: const {'hasUnpaidDebt': false});
    final provider = _provider(client);
    final debt = await provider.debt();

    expect(debt.hasUnpaidDebt, isFalse);
    expect(debt.rentalId, isNull);
  });
}
