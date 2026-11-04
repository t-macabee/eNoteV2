import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_form_screen.dart';
import 'package:enote_desktop/features/admin/address/address_list_screen.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/city/city_provider.dart';

import '../../../helpers.dart';

const _message = 'Prvo dodajte barem jedan grad u dijelu Referentni podaci.';

Future<ScriptedClient> _pumpList(
  WidgetTester tester, {
  required List<Map<String, dynamic>> cities,
}) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final httpClient = ScriptedClient((request) {
    final path = request.url.path;
    if (request.method == 'GET' && path.endsWith('/admin/cities')) {
      return jsonResponse({
        'items': cities,
        'page': 1,
        'pageSize': cities.length,
        'totalCount': cities.length,
      }, 200);
    }
    if (request.method == 'GET' && path.endsWith('/admin/addresses')) {
      return jsonResponse(const {
        'items': [],
        'page': 1,
        'pageSize': 20,
        'totalCount': 0,
      }, 200);
    }
    return jsonResponse(const {}, 200);
  });
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    httpClient: httpClient,
    tokenReader: () => fakeJwt(),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: httpClient,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<AuthState>.value(value: authState),
        ChangeNotifierProvider<AddressProvider>.value(
          value: AddressProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<CityProvider>.value(
          value: CityProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(home: AddressListScreen()),
    ),
  );
  await tester.pumpAndSettle();
  return httpClient;
}

void main() {
  testWidgets(
    'with an empty city list the add button shows the message and does not '
    'open AddressFormScreen',
    (WidgetTester tester) async {
      await _pumpList(tester, cities: const []);

      await tester.tap(find.widgetWithText(ElevatedButton, 'Dodaj'));
      await tester.pumpAndSettle();

      expect(find.text(_message), findsOneWidget);
      expect(find.byType(AddressFormScreen), findsNothing);
    },
  );

  testWidgets('with one city the add button opens AddressFormScreen', (
    WidgetTester tester,
  ) async {
    final httpClient = await _pumpList(
      tester,
      cities: const [
        {'id': 1, 'name': 'Sarajevo'},
      ],
    );

    await tester.tap(find.widgetWithText(ElevatedButton, 'Dodaj'));
    await tester.pumpAndSettle();

    expect(find.byType(AddressFormScreen), findsOneWidget);
    expect(find.text(_message), findsNothing);
    expect(
      httpClient.getUrls.where(
        (url) =>
            url.contains('admin/cities') &&
            Uri.parse(url).queryParameters['pageSize'] == '1',
      ),
      hasLength(1),
    );
  });
}
