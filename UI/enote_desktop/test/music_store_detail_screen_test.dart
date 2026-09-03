import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_provider.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_detail_screen.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';
import 'package:enote_desktop/features/admin/music_store/store_instrument_provider.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';

class _StoreRecordingHttpClient extends http.BaseClient {
  final List<String> requestedUrls = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final url = request.url.toString();
    requestedUrls.add(url);

    if (url.contains('admin/music-stores/1')) {
      final json = {
        'id': 1,
        'storeName': 'Muzička Kuća Sarajevo',
        'businessHours': '09:00 - 19:00',
        'phoneNumber': '+387 33 555 777',
        'imagePath': '/uploads/stores/store1.jpg',
        'addressId': 2,
        'addressStreet': 'Titova 10',
        'addressCity': 'Sarajevo',
      };
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode(json))),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    if (url.contains('admin/instrument-types')) {
      final json = {
        'items': [
          {'id': 1, 'type': 'Električna gitara', 'monthlyFee': 30.0},
          {'id': 2, 'type': 'Klavir', 'monthlyFee': 50.0},
        ],
        'page': 1,
        'pageSize': 100,
        'totalCount': 2,
      };
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode(json))),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    if (url.contains('instruments/public')) {
      final json = {
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
      };
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode(json))),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode({'items': [], 'page': 1, 'pageSize': 20, 'totalCount': 0}))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

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
    final httpClient = _StoreRecordingHttpClient();
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

    // Exactly one app bar — the embedded instrument grid must not render
    // its own nested Scaffold/AppBar on top of this screen's.
    expect(find.byType(AppBar), findsOneWidget);

    // Verify left panel details
    expect(find.text('Muzička Kuća Sarajevo'), findsWidgets);
    expect(find.text('Titova 10, Sarajevo'), findsOneWidget);
    expect(find.text('+387 33 555 777'), findsOneWidget);
    expect(find.text('09:00 - 19:00'), findsOneWidget);
    expect(find.widgetWithText(ElevatedButton, 'Uredi'), findsOneWidget);

    // Verify right panel instruments
    expect(find.text('Stratocaster Player'), findsOneWidget);
    expect(find.text('Fender'), findsOneWidget);
    expect(find.text('U1 Upright Piano'), findsOneWidget);
    expect(find.text('Yamaha'), findsOneWidget);

    // Verify section labels exist when "Svi instrumenti" is selected (grouping active)
    expect(find.byType(EntitySectionLabel), findsNWidgets(2));
    expect(find.widgetWithText(EntitySectionLabel, 'Električna gitara'), findsOneWidget);
    expect(find.widgetWithText(EntitySectionLabel, 'Klavir'), findsOneWidget);

    // Verify instrument fetch sent musicStoreId param
    expect(
      httpClient.requestedUrls.any((url) => url.contains('instruments/public') && url.contains('musicStoreId=1')),
      isTrue,
      reason: 'public instruments endpoint must be filtered by musicStoreId=1',
    );

    // Switch instrument type filter to "Klavir"
    await tester.tap(find.byType(DropdownButtonFormField<int?>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Klavir').last);
    await tester.pumpAndSettle();

    // When a specific type is selected, groupKeyOf is null -> no section labels
    expect(find.byType(EntitySectionLabel), findsNothing);

    // Verify instrument fetch sent instrumentTypeId param
    expect(
      httpClient.requestedUrls.any((url) => url.contains('instruments/public') && url.contains('instrumentTypeId=2')),
      isTrue,
      reason: 'selecting Klavir must pass instrumentTypeId=2 to the fetcher',
    );

    // Type in search bar
    await tester.enterText(find.byType(TextField), 'Yamaha');
    await tester.pumpAndSettle(const Duration(milliseconds: 400));

    expect(
      httpClient.requestedUrls.any((url) => url.contains('instruments/public') && url.contains('search=Yamaha')),
      isTrue,
      reason: 'typing in search bar must send search parameter',
    );
  });
}
