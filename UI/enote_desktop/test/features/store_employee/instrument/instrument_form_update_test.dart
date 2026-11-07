import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_form_screen.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/shop_instrument_type_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/shop_manufacturer_provider.dart';

import '../../../helpers.dart';

class _FormHost extends StatelessWidget {
  const _FormHost({required this.instrument});

  final InstrumentDto instrument;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: ElevatedButton(
          onPressed: () => Navigator.of(context).push(
            MaterialPageRoute(
              builder: (_) => InstrumentFormScreen(existing: instrument),
            ),
          ),
          child: const Text('Uredi'),
        ),
      ),
    );
  }
}

void main() {
  testWidgets('clearing the description sends a null description on update', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      tokenReader: () => fakeJwt(role: 'StoreEmployee', isManager: true),
    );
    final putBodies = <String?>[];
    final httpClient = ScriptedClient((request) {
      final url = request.url.toString();
      if (request.method == 'GET' && url.contains('shop/instrument-types')) {
        return jsonResponse(const {
          'items': [
            {'id': 1, 'type': 'Gitara'},
          ],
          'page': 1,
          'pageSize': 100,
          'totalCount': 1,
        }, 200);
      }
      if (request.method == 'GET' && url.contains('shop/manufacturers')) {
        return jsonResponse(const {
          'items': [
            {'id': 3, 'name': 'Fender'},
          ],
          'page': 1,
          'pageSize': 100,
          'totalCount': 1,
        }, 200);
      }
      if (request.method == 'PUT') {
        putBodies.add(request.body);
        return jsonResponse(const {
          'id': 1,
          'model': 'Stratocaster',
          'manufacturerId': 3,
          'manufacturerName': 'Fender',
          'description': null,
          'instrumentTypeId': 1,
          'instrumentType': 'Gitara',
          'monthlyFee': 45.0,
          'musicStore': 'Trgovina A',
          'isAvailable': true,
        }, 200);
      }
      return jsonResponse(const {
        'items': [],
        'page': 1,
        'pageSize': 100,
        'totalCount': 0,
      }, 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: httpClient,
    );
    final instrument = InstrumentDto(
      id: 1,
      model: 'Stratocaster',
      manufacturerId: 3,
      manufacturerName: 'Fender',
      description: 'Stari opis',
      instrumentTypeId: 1,
      instrumentType: 'Gitara',
      monthlyFee: 45,
      musicStore: 'Trgovina A',
      isAvailable: true,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<ShopInstrumentTypeProvider>.value(
            value: ShopInstrumentTypeProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider<ShopManufacturerProvider>.value(
            value: ShopManufacturerProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider<InstrumentProvider>.value(
            value: InstrumentProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(home: _FormHost(instrument: instrument)),
      ),
    );

    await tester.tap(find.text('Uredi'));
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextFormField, 'Opis'), '');
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(putBodies, hasLength(1));
    final body = jsonDecode(putBodies.single!) as Map<String, dynamic>;

    expect(body.keys.toSet(), {
      'model',
      'manufacturerId',
      'description',
      'instrumentTypeId',
    });
    expect(body.containsKey('description'), isTrue);
    expect(body['description'], isNull);
  });
}
