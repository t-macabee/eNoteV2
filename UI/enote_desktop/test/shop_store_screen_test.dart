import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_provider.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_screen.dart';
import 'package:enote_desktop/widgets/async_dropdown.dart';

String _base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

String _fakeJwt({
  String subject = '1',
  String username = 'testuser',
  String role = 'StoreEmployee',
  bool isManager = false,
}) {
  final header = _base64UrlSegment(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final payload = _base64UrlSegment(jsonEncode({
    'sub': subject,
    'unique_name': username,
    'role': role,
    if (isManager) 'is_manager': true,
    'exp': DateTime.now()
            .add(const Duration(days: 1))
            .millisecondsSinceEpoch ~/
        1000,
  }));
  return '$header.$payload.signature';
}

class _MockHttpClient extends http.BaseClient {
  final List<String> putBodies = [];
  final List<String> getUrls = [];
  final List<String> putUrls = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final url = request.url.toString();
    if (request.method == 'GET') {
      getUrls.add(url);
      if (url.contains('shop/store')) {
        final json = jsonEncode({
          'id': 10,
          'storeName': 'Muzička Prodavnica',
          'businessHours': '08:00 - 16:00',
          'phoneNumber': '+387 61 111 222',
          'addressId': 2,
          'addressStreet': 'Ferhadija 15',
          'addressCity': 'Sarajevo',
          'imagePath': '/images/store.jpg',
        });
        return http.StreamedResponse(
          Stream.value(utf8.encode(json)),
          200,
          headers: {'content-type': 'application/json'},
        );
      }
      if (url.contains('admin/addresses')) {
        final json = jsonEncode({
          'items': [
            {'id': 1, 'cityId': 1, 'city': 'Sarajevo', 'street': 'Maršala Tita', 'number': '1'},
            {'id': 2, 'cityId': 1, 'city': 'Sarajevo', 'street': 'Ferhadija', 'number': '15'},
          ],
          'page': 1,
          'pageSize': 100,
          'totalCount': 2,
        });
        return http.StreamedResponse(
          Stream.value(utf8.encode(json)),
          200,
          headers: {'content-type': 'application/json'},
        );
      }
    }

    if (request.method == 'PUT' && url.contains('shop/store')) {
      putUrls.add(url);
      if (request is http.Request) {
        putBodies.add(request.body);
      }
      final json = jsonEncode({
        'id': 10,
        'storeName': 'Ažurirana Prodavnica',
        'businessHours': '09:00 - 17:00',
        'phoneNumber': '+387 61 999 888',
        'addressId': 2,
        'addressStreet': 'Ferhadija 15',
        'addressCity': 'Sarajevo',
      });
      return http.StreamedResponse(
        Stream.value(utf8.encode(json)),
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

Widget _buildTestApp({
  required bool isManager,
  required _MockHttpClient httpClient,
}) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => _fakeJwt(role: 'StoreEmployee', isManager: isManager),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: httpClient,
  );
  final storeProvider = ShopStoreProvider(apiClient: apiClient);
  final addressProvider = AddressProvider(apiClient: apiClient);

  return MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthState>.value(value: authState),
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<ShopStoreProvider>.value(value: storeProvider),
      ChangeNotifierProvider<AddressProvider>.value(value: addressProvider),
    ],
    child: const MaterialApp(
      home: ShopStoreScreen(),
    ),
  );
}

void main() {
  testWidgets('ShopStoreScreen renders read-only for non-managers',
      (WidgetTester tester) async {
    final httpClient = _MockHttpClient();
    await tester.pumpWidget(_buildTestApp(isManager: false, httpClient: httpClient));
    await tester.pumpAndSettle();

    // Verify fields populated
    expect(find.text('Muzička Prodavnica'), findsOneWidget);
    expect(find.text('08:00 - 16:00'), findsOneWidget);
    expect(find.text('+387 61 111 222'), findsOneWidget);

    // Verify text fields are read-only
    final nameField = tester.widget<TextField>(
      find.descendant(
        of: find.widgetWithText(TextFormField, 'Naziv'),
        matching: find.byType(TextField),
      ),
    );
    expect(nameField.readOnly, isTrue);

    final hoursField = tester.widget<TextField>(
      find.descendant(
        of: find.widgetWithText(TextFormField, 'Radno vrijeme'),
        matching: find.byType(TextField),
      ),
    );
    expect(hoursField.readOnly, isTrue);

    final phoneField = tester.widget<TextField>(
      find.descendant(
        of: find.widgetWithText(TextFormField, 'Broj telefona'),
        matching: find.byType(TextField),
      ),
    );
    expect(phoneField.readOnly, isTrue);

    // Verify dropdown is disabled
    final dropdown = tester.widget<AsyncDropdown<AddressReferenceDto>>(
      find.byType(AsyncDropdown<AddressReferenceDto>),
    );
    expect(dropdown.enabled, isFalse);

    // Verify ImageField is not editable
    final imageField = tester.widget<ImageField>(find.byType(ImageField));
    expect(imageField.editable, isFalse);

    // Verify save button is not present
    expect(find.widgetWithText(FilledButton, 'Sačuvaj'), findsNothing);
  });

  testWidgets('ShopStoreScreen renders editable for managers and saves changes',
      (WidgetTester tester) async {
    final httpClient = _MockHttpClient();
    await tester.pumpWidget(_buildTestApp(isManager: true, httpClient: httpClient));
    await tester.pumpAndSettle();

    // Verify text fields are editable
    final nameField = tester.widget<TextField>(
      find.descendant(
        of: find.widgetWithText(TextFormField, 'Naziv'),
        matching: find.byType(TextField),
      ),
    );
    expect(nameField.readOnly, isFalse);

    final hoursField = tester.widget<TextField>(
      find.descendant(
        of: find.widgetWithText(TextFormField, 'Radno vrijeme'),
        matching: find.byType(TextField),
      ),
    );
    expect(hoursField.readOnly, isFalse);

    final phoneField = tester.widget<TextField>(
      find.descendant(
        of: find.widgetWithText(TextFormField, 'Broj telefona'),
        matching: find.byType(TextField),
      ),
    );
    expect(phoneField.readOnly, isFalse);

    // Verify dropdown is enabled
    final dropdown = tester.widget<AsyncDropdown<AddressReferenceDto>>(
      find.byType(AsyncDropdown<AddressReferenceDto>),
    );
    expect(dropdown.enabled, isTrue);

    // Verify ImageField is editable
    final imageField = tester.widget<ImageField>(find.byType(ImageField));
    expect(imageField.editable, isTrue);

    // Verify save button is present
    expect(find.widgetWithText(FilledButton, 'Sačuvaj'), findsOneWidget);

    // Edit a field and save
    await tester.enterText(
        find.widgetWithText(TextFormField, 'Naziv'), 'Ažurirana Prodavnica');
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    // Verify PUT request was made to shop/store
    expect(httpClient.putUrls, hasLength(1));
    expect(httpClient.putUrls.first, endsWith('shop/store'));
    final body = jsonDecode(httpClient.putBodies.first) as Map<String, dynamic>;
    expect(body['storeName'], equals('Ažurirana Prodavnica'));

    // Verify success snackbar
    expect(find.text('Uspješno sačuvano.'), findsOneWidget);
  });
}
