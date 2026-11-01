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

import '../../../helpers.dart';

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
        },
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

Future<void> _pumpDialog(WidgetTester tester, ApiClient apiClient) async {
  await tester.pumpWidget(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<MusicStoreProvider>.value(
          value: MusicStoreProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<StoreInstrumentProvider>.value(
          value: StoreInstrumentProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<AddressProvider>.value(
          value: AddressProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<InstrumentTypeProvider>.value(
          value: InstrumentTypeProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => TextButton(
              onPressed: () => MusicStoreDetailScreen.open(context, 1),
              child: const Text('Otvori prodavnicu'),
            ),
          ),
        ),
      ),
    ),
  );

  await tester.tap(find.text('Otvori prodavnicu'));
  await tester.pumpAndSettle();
}

Future<void> _pumpStore(WidgetTester tester, Map<String, dynamic> store) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(() {
    tester.view.resetPhysicalSize();
    tester.view.resetDevicePixelRatio();
  });

  final httpClient = ScriptedClient((request) {
    final url = request.url.toString();

    if (url.contains('admin/music-stores/1')) {
      return jsonResponse(store, 200);
    }

    return jsonResponse(const {
      'items': [],
      'page': 1,
      'pageSize': 20,
      'totalCount': 0,
    }, 200);
  });

  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
    httpClient: httpClient,
  );

  await _pumpDialog(tester, apiClient);
}

void main() {
  testWidgets(
    'MusicStoreDetailScreen shows the overview and the grouped instruments grid',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final httpClient = _client();
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
        httpClient: httpClient,
      );

      await _pumpDialog(tester, apiClient);

      expect(find.byType(MusicStoreDetailScreen), findsOneWidget);
      expect(find.byType(Scaffold), findsOneWidget);

      expect(find.text('Muzička Kuća Sarajevo'), findsWidgets);
      expect(find.text('Titova 10, Sarajevo'), findsOneWidget);
      expect(find.text('+387 33 555 777'), findsOneWidget);
      expect(find.text('09:00 - 19:00'), findsOneWidget);
      expect(find.widgetWithText(TextButton, 'Uredi'), findsOneWidget);

      await tester.tap(find.text('Instrumenti'));
      await tester.pumpAndSettle();

      expect(find.text('Stratocaster Player'), findsOneWidget);
      expect(find.text('Fender'), findsOneWidget);
      expect(find.text('U1 Upright Piano'), findsOneWidget);
      expect(find.text('Yamaha'), findsOneWidget);

      expect(find.byType(EntitySectionLabel), findsNWidgets(2));
      expect(
        find.widgetWithText(EntitySectionLabel, 'Električna gitara'),
        findsOneWidget,
      );
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

      await tester.tap(find.byType(DropdownButtonFormField<Object>));
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
    },
  );

  testWidgets(
    'a failed instrument-type load shows the filter error and retry loads it',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      var typeCalls = 0;
      final httpClient = ScriptedClient((request) {
        final url = request.url.toString();

        if (url.contains('admin/instrument-types')) {
          typeCalls++;
          if (typeCalls == 1) {
            return jsonResponse(const {'message': 'Greška'}, 500);
          }
          return jsonResponse(const {
            'items': [
              {'id': 2, 'type': 'Klavir', 'monthlyFee': 50.0},
            ],
            'page': 1,
            'pageSize': 100,
            'totalCount': 1,
          }, 200);
        }

        if (url.contains('admin/music-stores/1')) {
          return jsonResponse(const {
            'id': 1,
            'storeName': 'Muzička Kuća Sarajevo',
            'businessHours': '09:00 - 19:00',
            'addressCity': 'Sarajevo',
          }, 200);
        }

        return jsonResponse(const {
          'items': [],
          'page': 1,
          'pageSize': 24,
          'totalCount': 0,
        }, 200);
      });

      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
        httpClient: httpClient,
      );

      await _pumpDialog(tester, apiClient);

      await tester.tap(find.text('Instrumenti'));
      await tester.pumpAndSettle();

      expect(find.text('Neuspjelo učitavanje.'), findsOneWidget);

      await tester.tap(find.byTooltip('Pokušaj ponovo'));
      await tester.pumpAndSettle();

      expect(find.text('Neuspjelo učitavanje.'), findsNothing);
      expect(typeCalls, 2);

      await tester.tap(find.byType(DropdownButtonFormField<Object>));
      await tester.pumpAndSettle();
      expect(find.text('Klavir'), findsOneWidget);
    },
  );

  testWidgets('the close button closes the dialog', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: _client(),
    );

    await _pumpDialog(tester, apiClient);

    expect(find.byType(MusicStoreDetailScreen), findsOneWidget);
    expect(find.byTooltip('Zatvori'), findsOneWidget);

    await tester.tap(find.byTooltip('Zatvori'));
    await tester.pumpAndSettle();

    expect(find.byType(MusicStoreDetailScreen), findsNothing);
  });

  testWidgets(
    'the instrument-type filter loads every page of instrument types',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final httpClient = ScriptedClient((request) {
        final url = request.url.toString();

        if (url.contains('admin/instrument-types')) {
          final page = int.parse(request.url.queryParameters['page'] ?? '1');
          return jsonResponse({
            'items': [
              for (var i = 0; i < (page == 1 ? 100 : 50); i++)
                {
                  'id': page * 1000 + i,
                  'type': 'Tip ${page * 1000 + i}',
                  'monthlyFee': 50.0,
                },
            ],
            'page': page,
            'pageSize': 100,
            'totalCount': 150,
          }, 200);
        }

        if (url.contains('admin/music-stores/1')) {
          return jsonResponse(const {
            'id': 1,
            'storeName': 'Muzička Kuća Sarajevo',
            'businessHours': '09:00 - 19:00',
            'addressCity': 'Sarajevo',
          }, 200);
        }

        return jsonResponse(const {
          'items': [],
          'page': 1,
          'pageSize': 24,
          'totalCount': 0,
        }, 200);
      });

      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
        httpClient: httpClient,
      );

      await _pumpDialog(tester, apiClient);

      await tester.tap(find.text('Instrumenti'));
      await tester.pumpAndSettle();

      final typeUrls = httpClient.getUrls
          .where((url) => url.contains('admin/instrument-types'))
          .toList();
      expect(typeUrls, hasLength(2));
      expect(typeUrls.first, contains('page=1'));
      expect(typeUrls.last, contains('page=2'));

      final dropdown = tester.widget<DropdownButton<Object>>(
        find.byType(DropdownButton<Object>),
      );
      expect(dropdown.items, hasLength(151));
      expect(dropdown.items!.first.value, isNull);
      expect(dropdown.items!.last.value, 2049);
    },
  );

  testWidgets('Uredi and Obriši are in the app bar', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: _client(),
    );

    await _pumpDialog(tester, apiClient);

    expect(
      find.descendant(of: find.byType(AppBar), matching: find.text('Uredi')),
      findsOneWidget,
    );
    expect(
      find.descendant(of: find.byType(AppBar), matching: find.text('Obriši')),
      findsOneWidget,
    );
  });

  testWidgets('overview shows address, phone and hours', (
    WidgetTester tester,
  ) async {
    await _pumpStore(tester, const {
      'id': 1,
      'storeName': 'Muzička Kuća Sarajevo',
      'businessHours': '09:00 - 19:00',
      'phoneNumber': '+387 33 555 777',
      'addressStreet': 'Titova 10',
      'addressCity': 'Sarajevo',
    });

    expect(find.text('Titova 10, Sarajevo'), findsOneWidget);
    expect(find.text('+387 33 555 777'), findsOneWidget);
    expect(find.text('09:00 - 19:00'), findsOneWidget);
  });

  testWidgets('overview shows dashes for empty values', (
    WidgetTester tester,
  ) async {
    await _pumpStore(tester, const {
      'id': 1,
      'storeName': 'Prazna prodavnica',
      'businessHours': '',
    });

    expect(find.text('-'), findsNWidgets(3));
  });

  testWidgets('overview is not vertically centered', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: _client(),
    );

    await _pumpDialog(tester, apiClient);

    final top = tester.getTopLeft(find.text('Podaci o prodavnici')).dy;
    expect(top, lessThan(200));
  });

  testWidgets(
    'music store detail screen shows two tabs Pregled and Instrumenti',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
        httpClient: _client(),
      );

      await _pumpDialog(tester, apiClient);

      expect(find.text('Pregled'), findsOneWidget);
      expect(find.text('Instrumenti'), findsOneWidget);
    },
  );

  testWidgets('delete closes the screen and refreshes the list', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final httpClient = ScriptedClient((request) {
      final url = request.url.toString();
      if (request.method == 'DELETE' && url.contains('admin/music-stores/1')) {
        return jsonResponse(const {}, 200);
      }
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
      return jsonResponse(const {
        'items': [],
        'page': 1,
        'pageSize': 20,
        'totalCount': 0,
      }, 200);
    });

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: httpClient,
    );

    await _pumpDialog(tester, apiClient);

    await tester.ensureVisible(find.text('Obriši'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Obriši'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(find.text('Prodavnica je obrisana.'), findsOneWidget);
    expect(find.byType(MusicStoreDetailScreen), findsNothing);
  });
}
