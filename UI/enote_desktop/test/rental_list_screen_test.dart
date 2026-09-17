import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_list_screen.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';

import 'helpers.dart';

class _RecordingClient extends http.BaseClient {
  final List<Uri> instrumentUrls = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final path = request.url.path;
    if (request.method == 'GET' && path.contains('shop/instruments')) {
      instrumentUrls.add(request.url);
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode({
          'items': [],
          'page': 1,
          'pageSize': 100,
          'totalCount': 0,
        }))),
        200,
        headers: {'content-type': 'application/json'},
      );
    }
    if (request.method == 'GET' && path.contains('shop/rentals')) {
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode({
          'items': [
            {
              'id': 7,
              'instrumentId': 1,
              'musicStoreId': 1,
              'studentProfileId': 1,
              'studentUserId': 2,
              'studentName': 'Student Test',
              'instrumentModel': 'Stratocaster',
              'instrumentType': 'Gitara',
              'storeName': 'Shop',
              'rentalStatus': 'Active',
              'requestedAt': DateTime(2026, 1, 5).toIso8601String(),
              'fee': 10.0,
              'isProrated': false,
              'totalFee': 5.0,
              'isPaid': false,
            }
          ],
          'page': 1,
          'pageSize': 20,
          'totalCount': 1,
        }))),
        200,
        headers: {'content-type': 'application/json'},
      );
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode('{}')),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

Future<void> _pumpList(WidgetTester tester, _RecordingClient client) async {
  tester.view.physicalSize = const Size(1400, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    httpClient: client,
    tokenReader: () => fakeJwt(username: 'test', role: 'StoreEmployee'),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: client,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<InstrumentProvider>.value(
          value: InstrumentProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<RentalProvider>.value(
          value: RentalProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(
        home: Scaffold(body: RentalListScreen()),
      ),
    ),
  );
  await tester.pump();
  await pumpPastDebounce(tester);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
      'F4-04: instrument filter query matches pagedQuery(1, 100) output',
      (tester) async {
    final client = _RecordingClient();
    await _pumpList(tester, client);

    expect(client.instrumentUrls, isNotEmpty);
    final expected = pagedQuery(1, 100, '')
        .map((k, v) => MapEntry(k, v.toString()));
    expect(client.instrumentUrls.first.queryParameters, equals(expected));
  });

  testWidgets('F4-05: fee column renders with the KM suffix',
      (tester) async {
    final client = _RecordingClient();
    await _pumpList(tester, client);

    expect(find.text('10.00 KM / 5.00 KM'), findsOneWidget);
  });
}
