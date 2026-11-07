import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/shop_instrument_type_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/shop_manufacturer_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_form_screen.dart';

import '../../../helpers.dart';

void main() {
  testWidgets('after reset the instrument type dropdown visually clears and '
      'required field validator blocks submission without a stale value', (
    WidgetTester tester,
  ) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final postedBodies = <String?>[];
    var nextId = 1;
    final httpClient = ScriptedClient((request) {
      final url = request.url.toString();
      if (request.method == 'GET' && url.contains('shop/manufacturers')) {
        return jsonResponse(const {
          'items': [
            {'id': 1, 'name': 'Test proizvođač'},
          ],
          'page': 1,
          'pageSize': 100,
          'totalCount': 1,
        }, 200);
      }
      if (request.method == 'GET') {
        return jsonResponse(const {
          'items': [
            {'id': 1, 'type': 'Gitara'},
            {'id': 2, 'type': 'Klavir'},
          ],
          'page': 1,
          'pageSize': 100,
          'totalCount': 2,
        }, 200);
      }
      postedBodies.add(request.body);
      return jsonResponse({
        'id': nextId++,
        'model': 'Test model',
        'manufacturerId': 1,
        'manufacturerName': 'Test proizvođač',
        'description': 'Test opis',
        'instrumentTypeId': 1,
        'instrumentType': 'Gitara',
        'musicStore': 'Trgovina A',
        'isAvailable': true,
      }, 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: httpClient,
    );
    final instrumentProvider = InstrumentProvider(apiClient: apiClient);
    final instrumentTypeProvider = ShopInstrumentTypeProvider(
      apiClient: apiClient,
    );
    final manufacturerProvider = ShopManufacturerProvider(apiClient: apiClient);

    await tester.pumpWidget(
      ChangeNotifierProvider<ShopManufacturerProvider>.value(
        value: manufacturerProvider,
        child: ChangeNotifierProvider<ShopInstrumentTypeProvider>.value(
          value: instrumentTypeProvider,
          child: ChangeNotifierProvider<InstrumentProvider>.value(
            value: instrumentProvider,
            child: const MaterialApp(home: InstrumentFormScreen()),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final dropdowns = find.byType(DropdownButtonFormField<Object>);

    Future<void> fillModel() async {
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Model'),
        'Test model',
      );
    }

    Future<void> pickManufacturer(String name) async {
      await tester.tap(dropdowns.at(0));
      await tester.pumpAndSettle();
      await tester.tap(find.text(name).last);
      await tester.pumpAndSettle();
    }

    Future<void> pickInstrumentType(String typeName) async {
      await tester.tap(dropdowns.at(1));
      await tester.pumpAndSettle();
      await tester.tap(find.text(typeName).last);
      await tester.pumpAndSettle();
    }

    Future<void> save() async {
      await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
      await tester.pumpAndSettle();
    }

    await fillModel();
    await pickManufacturer('Test proizvođač');
    await pickInstrumentType('Gitara');
    await save();

    expect(postedBodies, hasLength(1));
    final firstBody = jsonDecode(postedBodies[0]!) as Map<String, dynamic>;
    expect(firstBody['instrumentTypeId'], equals(1));
    expect(firstBody['manufacturerId'], equals(1));
    expect(firstBody['model'], equals('Test model'));

    await fillModel();
    await save();

    expect(
      find.text('Tip instrumenta je obavezan'),
      findsOneWidget,
      reason: 'the instrument type dropdown must show validator error when untouched after reset',
    );
    expect(
      postedBodies,
      hasLength(1),
      reason: 'no stale instrumentTypeId may reach a second POST',
    );
  });
}
