
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_provider.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_detail_screen.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';
import 'package:enote_desktop/features/admin/music_store/store_instrument_provider.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';

import 'helpers.dart';

ScriptedClient _client() => ScriptedClient((request) {
  final url = request.url.toString();

  if (url.contains('admin/music-stores/1')) {
    return jsonResponse(const {
      'id': 1,
      'storeName': 'Muzička Kuća Sarajevo',
      'businessHours': '09:00 - 19:00',
      'phoneNumber': '+387 33 555 777',
      'imagePath': '/uploads/stores/store1.jpg',
      'addressId': 2,
      'addressStreet': 'Titova 10',
      'addressCity': 'Sarajevo',
    }, 200);
  }

  if (url.contains('admin/instrument-types')) {
    return jsonResponse(const {
      'items': [
        {'id': 1, 'type': 'Električna gitara', 'monthlyFee': 30.0},
        {'id': 2, 'type': 'Klavir', 'monthlyFee': 50.0},
      ],
      'page': 1,
      'pageSize': 100,
      'totalCount': 2,
    }, 200);
  }

  if (url.contains('instruments/public')) {
    return jsonResponse(const {
      'items': [
        {
          'id': 101,
          'model': 'Stratocaster Player',
          'manufacturer': 'Fender',
          'description': 'Solid body electric guitar',
          'imagePath': null,
          'instrumentTypeId': 1,
          'instrumentType': 'Električna gitara',
          'musicStore': 'Muzička Kuća Sarajevo',
          'isAvailable': true,
        },
        {
          'id': 102,
          'model': 'U1 Upright Piano',
          'manufacturer': 'Yamaha',
          'description': 'Acoustic piano',
          'imagePath': null,
          'instrumentTypeId': 2,
          'instrumentType': 'Klavir',
          'musicStore': 'Muzička Kuća Sarajevo',
          'isAvailable': false,
        }
      ],
      'page': 1,
      'pageSize': 24,
      'totalCount': 2,
    }, 200);
  }

  return jsonResponse(const {
    'items': [],
    'page': 1,
    'pageSize': 20,
    'totalCount': 0,
  }, 200);
});

void main() {
  testWidgets('MusicStoreDetailScreen loads store details on left and instruments grid on right with dual-purpose grouping',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final httpClient = _client();
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: httpClient,
    );
    final musicStoreProvider = MusicStoreProvider(apiClient: apiClient);
    final storeInstrumentProvider = StoreInstrumentProvider(apiClient: apiClient);
    final addressProvider = AddressProvider(apiClient: apiClient);
    final instrumentTypeProvider = InstrumentTypeProvider(apiClient: apiClient);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<MusicStoreProvider>.value(value: musicStoreProvider),
          ChangeNotifierProvider<StoreInstrumentProvider>.value(value: storeInstrumentProvider),
          ChangeNotifierProvider<AddressProvider>.value(value: addressProvider),
          ChangeNotifierProvider<InstrumentTypeProvider>.value(value: instrumentTypeProvider),
        ],
        child: const MaterialApp(
          home: MusicStoreDetailScreen(storeId: 1),
        ),
      ),
    );

    // Initial frame + async loads
    await tester.pump();
    await tester.pumpAndSettle();

    // Exactly one scaffold — the embedded instrument grid must not render
    // its own nested Scaffold/AppBar on top of this screen's.
    expect(find.byType(Scaffold), findsOneWidget);

    // Verify left panel details
    expect(find.text('Muzička Kuća Sarajevo'), findsWidgets);
    expect(find.text('Titova 10, Sarajevo'), findsOneWidget);
    expect(find.text('+387 33 555 777'), findsOneWidget);
    expect(find.text('09:00 - 19:00'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'Uredi'), findsOneWidget);

    // Verify right panel instruments
    expect(find.text('Stratocaster Player'), findsOneWidget);
    expect(find.text('Fender'), findsOneWidget);
    expect(find.text('U1 Upright Piano'), findsOneWidget);
    expect(find.text('Yamaha'), findsOneWidget);

    // Verify section labels exist when "Svi instrumenti" is selected (grouping active)
    expect(find.byType(EntitySectionLabel), findsNWidgets(2));
    expect(find.widgetWithText(EntitySectionLabel, 'Električna gitara'), findsOneWidget);
    expect(find.widgetWithText(EntitySectionLabel, 'Klavir'), findsOneWidget);

    expect(
      httpClient.requestedUrls.any(
        (url) =>
            url.contains('instruments/public') &&
            url.contains('search.musicStoreId=1') &&
            url.contains('search.page=1') &&
            url.contains('search.pageSize=24') &&
            url.contains('search.includeTotalCount=true'),
      ),
      isTrue,
      reason: 'public instruments endpoint must use search. prefixed keys',
    );

    await tester.tap(find.byType(DropdownButtonFormField<int?>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Klavir').last);
    await tester.pumpAndSettle();

    expect(find.byType(EntitySectionLabel), findsNothing);

    expect(
      httpClient.requestedUrls.any(
        (url) =>
            url.contains('instruments/public') &&
            url.contains('search.musicStoreId=1') &&
            url.contains('search.instrumentTypeId=2'),
      ),
      isTrue,
      reason: 'selecting Klavir must pass search.instrumentTypeId=2',
    );

    await tester.enterText(find.byType(TextField), 'Yamaha');
    await tester.pumpAndSettle(const Duration(milliseconds: 400));

    expect(
      httpClient.requestedUrls.any(
        (url) =>
            url.contains('instruments/public') &&
            url.contains('search.musicStoreId=1') &&
            url.contains('search.search=Yamaha'),
      ),
      isTrue,
      reason: 'search request must preserve search.musicStoreId=1 alongside search.search=Yamaha',
    );
  });
}
