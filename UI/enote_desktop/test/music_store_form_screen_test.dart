import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_form_screen.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';

class _StoreFormRecordingHttpClient extends http.BaseClient {
  final List<String?> postedBodies = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request.method == 'GET' && request.url.toString().contains('admin/addresses')) {
      final responseJson = jsonEncode({
        'items': [
          {'id': 1, 'cityId': 1, 'city': 'Sarajevo', 'street': 'Maršala Tita', 'number': '1'},
          {'id': 2, 'cityId': 1, 'city': 'Sarajevo', 'street': 'Ferhadija', 'number': '15'},
        ],
        'page': 1,
        'pageSize': 100,
        'totalCount': 2,
      });
      return http.StreamedResponse(
        Stream.value(utf8.encode(responseJson)),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    final body = request is http.Request ? request.body : null;
    postedBodies.add(body);

    final responseJson = jsonEncode({
      'id': 10,
      'storeName': 'Nova Prodavnica',
      'businessHours': '08:00 - 16:00',
      'phoneNumber': '+387 61 111 222',
      'addressId': 2,
      'addressStreet': 'Ferhadija 15',
      'addressCity': 'Sarajevo',
    });
    return http.StreamedResponse(
      Stream.value(utf8.encode(responseJson)),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('MusicStoreFormScreen includes phone number and address dropdown and posts them',
      (WidgetTester tester) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final httpClient = _StoreFormRecordingHttpClient();
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: httpClient,
    );
    final musicStoreProvider = MusicStoreProvider(apiClient: apiClient);
    final addressProvider = AddressProvider(apiClient: apiClient);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<MusicStoreProvider>.value(value: musicStoreProvider),
          ChangeNotifierProvider<AddressProvider>.value(value: addressProvider),
        ],
        child: const MaterialApp(
          home: MusicStoreFormScreen(),
        ),
      ),
    );

    await tester.pump();
    await tester.pumpAndSettle();

    // Fill fields
    await tester.enterText(find.widgetWithText(TextFormField, 'Naziv'), 'Nova Prodavnica');
    await tester.enterText(find.widgetWithText(TextFormField, 'Radno vrijeme'), '08:00 - 16:00');
    await tester.enterText(find.widgetWithText(TextFormField, 'Broj telefona'), '+387 61 111 222');

    // Pick address from dropdown
    await tester.tap(find.byType(DropdownButtonFormField<Object>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Ferhadija 15, Sarajevo').last);
    await tester.pumpAndSettle();

    // Save
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(httpClient.postedBodies, hasLength(1));
    final body = jsonDecode(httpClient.postedBodies.first!) as Map<String, dynamic>;
    expect(body['storeName'], equals('Nova Prodavnica'));
    expect(body['businessHours'], equals('08:00 - 16:00'));
    expect(body['phoneNumber'], equals('+387 61 111 222'));
    expect(body['addressId'], equals(2));
  });
}
