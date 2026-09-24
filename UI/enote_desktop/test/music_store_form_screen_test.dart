import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_form_screen.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';

import 'helpers.dart';

ScriptedClient _client() => ScriptedClient((request) {
  if (request.method == 'GET' &&
      request.url.toString().contains('admin/addresses')) {
    return jsonResponse(const {
      'items': [
        {
          'id': 1,
          'cityId': 1,
          'city': 'Sarajevo',
          'street': 'Maršala Tita',
          'number': '1'
        },
        {
          'id': 2,
          'cityId': 1,
          'city': 'Sarajevo',
          'street': 'Ferhadija',
          'number': '15'
        },
      ],
      'page': 1,
      'pageSize': 100,
      'totalCount': 2,
    }, 200);
  }

  return jsonResponse(const {
    'id': 10,
    'storeName': 'Nova Prodavnica',
    'businessHours': '08:00 - 16:00',
    'phoneNumber': '+387 61 111 222',
    'addressId': 2,
    'addressStreet': 'Ferhadija 15',
    'addressCity': 'Sarajevo',
  }, 200);
});

void main() {
  testWidgets('MusicStoreFormScreen includes phone number and address dropdown and posts them',
      (WidgetTester tester) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final httpClient = _client();
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
    final body = jsonDecode(httpClient.postedBodies.first) as Map<String, dynamic>;
    expect(body['storeName'], equals('Nova Prodavnica'));
    expect(body['businessHours'], equals('08:00 - 16:00'));
    expect(body['phoneNumber'], equals('+387 61 111 222'));
    expect(body['addressId'], equals(2));
  });

  testWidgets('MusicStoreFormScreen dialog mode renders Otkaži and Sačuvaj in actions row, and edit mode shows image auto-save label',
      (WidgetTester tester) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final httpClient = _client();
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: httpClient,
    );
    final musicStoreProvider = MusicStoreProvider(apiClient: apiClient);
    final addressProvider = AddressProvider(apiClient: apiClient);

    final existingStore = MusicStoreDto(
      id: 5,
      storeName: 'Postojeća Prodavnica',
      businessHours: '09:00 - 17:00',
      phoneNumber: '+387 33 000 111',
      addressId: 1,
      addressStreet: 'Maršala Tita 1',
      addressCity: 'Sarajevo',
      imagePath: '/images/store5.jpg',
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<MusicStoreProvider>.value(value: musicStoreProvider),
          ChangeNotifierProvider<AddressProvider>.value(value: addressProvider),
        ],
        child: MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: Center(
                child: ElevatedButton(
                  onPressed: () => EntityFormScaffold.showAsDialog(
                    context,
                    builder: (_) => MusicStoreFormScreen(
                      existing: existingStore,
                      presentation: EntityFormPresentation.dialog,
                    ),
                  ),
                  child: const Text('Open Dialog'),
                ),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open Dialog'));
    await tester.pumpAndSettle();

    // Verify dialog title
    expect(find.text('Uredi prodavnicu'), findsOneWidget);

    // Verify image auto-save helper text is present in edit mode
    expect(find.text('Slika se automatski sprema prilikom odabira.'), findsOneWidget);

    // Verify actions row contains Otkaži and Sačuvaj
    expect(find.widgetWithText(TextButton, 'Otkaži'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Sačuvaj'), findsOneWidget);

    // Tapping Otkaži closes the dialog
    await tester.tap(find.widgetWithText(TextButton, 'Otkaži'));
    await tester.pumpAndSettle();

    expect(find.text('Uredi prodavnicu'), findsNothing);
  });

  testWidgets('MusicStoreFormScreen shows error message and does not submit when phone format is invalid',
      (WidgetTester tester) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final httpClient = _client();
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

    await tester.enterText(find.widgetWithText(TextFormField, 'Naziv'), 'Nova Prodavnica');
    await tester.enterText(find.widgetWithText(TextFormField, 'Radno vrijeme'), '08:00 - 16:00');
    await tester.enterText(find.widgetWithText(TextFormField, 'Broj telefona'), 'abc');

    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(
      find.text(
        'Unesite broj telefona sa 6 do 15 cifara, opcionalno sa + na početku; razmaci su dozvoljeni (npr. +387 61 123 456).',
      ),
      findsOneWidget,
    );
    expect(httpClient.postedBodies, isEmpty);
  });
}
