import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/shop_instrument_type_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_form_screen.dart';

import 'helpers.dart';

void main() {
  testWidgets(
    'after reset the instrument type dropdown visually clears and '
    'required field validator blocks submission without a stale value',
    (WidgetTester tester) async {
      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final postedBodies = <String?>[];
      var nextId = 1;
      final httpClient = ScriptedClient((request) {
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
          'manufacturer': 'Test proizvođač',
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
      final instrumentTypeProvider =
          ShopInstrumentTypeProvider(apiClient: apiClient);

      await tester.pumpWidget(
        ChangeNotifierProvider<ShopInstrumentTypeProvider>.value(
          value: instrumentTypeProvider,
          child: ChangeNotifierProvider<InstrumentProvider>.value(
            value: instrumentProvider,
            child: const MaterialApp(home: InstrumentFormScreen()),
          ),
        ),
      );

      Future<void> fillBasicFields() async {
        await tester.enterText(find.widgetWithText(TextFormField, 'Model'), 'Test model');
        await tester.enterText(
            find.widgetWithText(TextFormField, 'Proizvođač'), 'Test proizvođač');
      }

      Future<void> pickInstrumentType(String typeName) async {
        await tester.tap(find.byType(DropdownButtonFormField<Object>));
        await tester.pumpAndSettle();
        await tester.tap(find.text(typeName).last);
        await tester.pumpAndSettle();
      }

      Future<void> save() async {
        await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
        await tester.pumpAndSettle();
      }

      // First save: fill Model, Proizvođač, and pick type "Gitara".
      await fillBasicFields();
      await pickInstrumentType('Gitara');
      await save();

      expect(postedBodies, hasLength(1));
      final firstBody =
          jsonDecode(postedBodies[0]!) as Map<String, dynamic>;
      expect(firstBody['instrumentTypeId'], equals(1));
      expect(firstBody['model'], equals('Test model'));
      expect(firstBody['manufacturer'], equals('Test proizvođač'));

      // Second save: only re-fill Model and Proizvođač, type dropdown left untouched.
      // The form's onReset cleared _selectedInstrumentTypeId to null, so the dropdown
      // should also show empty (not the stale "Gitara" from before the fix).
      await fillBasicFields();
      await save();

      expect(
        find.text('Tip instrumenta je obavezan'),
        findsOneWidget,
        reason: 'the instrument type dropdown must show validator error when untouched after reset',
      );
      expect(postedBodies, hasLength(1),
          reason: 'no stale instrumentTypeId may reach a second POST');
    },
  );
}