
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/store/shop_address_provider.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_form_screen.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_provider.dart';

import 'helpers.dart';

ScriptedClient _client() => ScriptedClient((request) {
  final url = request.url.toString();
  if (request.method == 'PUT' && url.contains('shop/store')) {
    return jsonResponse(const {
      'id': 10,
      'storeName': 'Shop',
      'businessHours': '08-16',
      'phoneNumber': '+38761111222',
      'addressId': 2,
      'addressStreet': 'Ferhadija 15',
      'addressCity': 'Sarajevo',
    }, 200);
  }
  return jsonResponse(const {
    'items': [],
    'page': 1,
    'pageSize': 100,
    'totalCount': 0,
  }, 200);
});

Future<ScriptedClient> _pumpForm(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1400, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final client = _client();
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(role: 'StoreEmployee', isManager: true),
    httpClient: client,
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: client,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<ShopStoreProvider>.value(
          value: ShopStoreProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<ShopAddressProvider>.value(
          value: ShopAddressProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: ShopStoreFormScreen(
            store: MusicStoreDto.fromJson({
              'id': 10,
              'storeName': 'Shop',
              'businessHours': '08-16',
              'phoneNumber': '+38761111222',
              'addressId': 2,
              'addressStreet': 'Ferhadija 15',
              'addressCity': 'Sarajevo',
            }),
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return client;
}

void main() {
  testWidgets('F4-13: malformed phone shows the validator text',
      (tester) async {
    final client = await _pumpForm(tester);

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Broj telefona'),
      'abc',
    );
    await tester.pump();
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(find.text('Unesite važeći broj telefona.'), findsOneWidget);
    expect(client.putUrls.length, equals(0));
  });

  testWidgets('F4-13: empty phone stays optional', (tester) async {
    final client = await _pumpForm(tester);

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Broj telefona'),
      '',
    );
    await tester.pump();
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(find.text('Unesite važeći broj telefona.'), findsNothing);
    expect(client.putUrls.length, equals(1));
  });
}
