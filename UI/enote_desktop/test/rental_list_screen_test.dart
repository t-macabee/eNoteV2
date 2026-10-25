
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_detail_dialog.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_list_screen.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';

import 'helpers.dart';

Map<String, dynamic> _rentalItem() => {
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
      'totalFee': 5.0,
      'isPaid': false,
    };

ScriptedClient _client() => ScriptedClient((request) {
  final path = request.url.path;
  if (request.method == 'GET' && path.endsWith('/shop/rentals/7')) {
    return jsonResponse(_rentalItem(), 200);
  }
  if (request.method == 'GET' && path.contains('shop/instruments')) {
    return jsonResponse(const {
      'items': [],
      'page': 1,
      'pageSize': 100,
      'totalCount': 0,
    }, 200);
  }
  if (request.method == 'GET' && path.contains('shop/rentals')) {
    return jsonResponse({
      'items': [_rentalItem()],
      'page': 1,
      'pageSize': 20,
      'totalCount': 1,
    }, 200);
  }
  return jsonResponse(const {}, 200);
});

Future<void> _pumpList(WidgetTester tester, ScriptedClient client) async {
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
    final client = _client();
    await _pumpList(tester, client);

    final instrumentUrls =
        client.getUrls.where((u) => u.contains('shop/instruments')).toList();
    expect(instrumentUrls, isNotEmpty);
    final expected = pagedQuery(1, 100, '')
        .map((k, v) => MapEntry(k, v.toString()));
    expect(Uri.parse(instrumentUrls.first).queryParameters, equals(expected));
  });

  testWidgets('F4-05: fee column renders with the KM suffix',
      (tester) async {
    final client = _client();
    await _pumpList(tester, client);

    expect(find.text('10.00 KM / 5.00 KM'), findsOneWidget);
  });

  testWidgets('opening a rental row shows a dialog, not a new page',
      (tester) async {
    final client = _client();
    await _pumpList(tester, client);

    await tester.tap(find.byIcon(Icons.edit));
    await tester.pumpAndSettle();

    expect(find.byType(Dialog), findsOneWidget);
    expect(find.byType(RentalDetailDialog), findsOneWidget);
  });

  testWidgets('the instrument filter loads every page of instruments', (
    tester,
  ) async {
    final client = ScriptedClient((request) {
      final path = request.url.path;
      if (path.contains('shop/instruments')) {
        final page = int.parse(request.url.queryParameters['page'] ?? '1');
        return jsonResponse({
          'items': [
            for (var i = 0; i < (page == 1 ? 100 : 50); i++)
              {
                'id': page * 1000 + i,
                'model': 'Instrument ${page * 1000 + i}',
                'manufacturer': 'Fender',
                'instrumentTypeId': 1,
                'instrumentType': 'Gitara',
                'musicStore': 'Shop',
                'isAvailable': true,
              },
          ],
          'page': page,
          'pageSize': 100,
          'totalCount': 150,
        }, 200);
      }
      if (path.contains('shop/rentals')) {
        return jsonResponse(const {
          'items': [],
          'page': 1,
          'pageSize': 20,
          'totalCount': 0,
        }, 200);
      }
      return jsonResponse(const {}, 200);
    });

    await _pumpList(tester, client);

    final instrumentUrls =
        client.getUrls.where((u) => u.contains('shop/instruments')).toList();
    expect(instrumentUrls, hasLength(2));
    expect(instrumentUrls.first, contains('page=1'));
    expect(instrumentUrls.last, contains('page=2'));

    final dropdown = tester.widget<DropdownButton<Object>>(
      find.byType(DropdownButton<Object>),
    );
    expect(dropdown.items, hasLength(151));
    expect((dropdown.items!.first.child as Text).data, 'Svi instrumenti');
    expect((dropdown.items!.last.child as Text).data, 'Instrument 2049');
  });
}
