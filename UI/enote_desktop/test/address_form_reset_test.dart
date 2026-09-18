import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/address/address_form_screen.dart';
import 'package:enote_desktop/features/admin/city/city_provider.dart';

import 'helpers.dart';

void main() {
  testWidgets(
    'after reset the city dropdown visually clears and '
    'required field validator blocks submission without a stale value',
    (WidgetTester tester) async {
      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final postedBodies = <String?>[];
      var nextId = 1;
      final httpClient = ScriptedClient((request) {
        if (request.method == 'GET') {
          return jsonResponse(const {
            'items': [
              {'id': 1, 'name': 'Sarajevo'},
              {'id': 2, 'name': 'Mostar'},
            ],
            'page': 1,
            'pageSize': 100,
            'totalCount': 2,
          }, 200);
        }
        postedBodies.add(request.body);
        return jsonResponse({
          'id': nextId++,
          'cityId': 1,
          'city': 'Sarajevo',
          'street': 'Test ulica',
          'number': '1',
        }, 200);
      });
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: httpClient,
      );
      final addressProvider = AddressProvider(apiClient: apiClient);
      final cityProvider = CityProvider(apiClient: apiClient);

      await tester.pumpWidget(
        ChangeNotifierProvider<CityProvider>.value(
          value: cityProvider,
          child: ChangeNotifierProvider<AddressProvider>.value(
            value: addressProvider,
            child: const MaterialApp(home: AddressFormScreen()),
          ),
        ),
      );

      Future<void> fillBasicFields() async {
        await tester.enterText(find.widgetWithText(TextFormField, 'Ulica'), 'Ulica A');
        await tester.enterText(find.widgetWithText(TextFormField, 'Broj'), '10');
      }

      Future<void> pickCity(String cityName) async {
        await tester.tap(find.byType(DropdownButtonFormField<Object>));
        await tester.pumpAndSettle();
        await tester.tap(find.text(cityName).last);
        await tester.pumpAndSettle();
      }

      Future<void> save() async {
        await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
        await tester.pumpAndSettle();
      }

      // First save: fill all fields including city.
      await fillBasicFields();
      await pickCity('Sarajevo');
      await save();

      expect(postedBodies, hasLength(1));
      final firstBody = jsonDecode(postedBodies[0]!) as Map<String, dynamic>;
      expect(firstBody['cityId'], equals(1));
      expect(firstBody['street'], equals('Ulica A'));
      expect(firstBody['number'], equals('10'));

      // Second save: only re-fill the text fields, city dropdown left untouched.
      // The form's onReset cleared _selectedCityId to null, so the dropdown
      // should also show empty (not the stale "Sarajevo" from before the fix).
      await fillBasicFields();
      await save();

      expect(
        find.text('Grad je obavezan'),
        findsOneWidget,
        reason: 'the city dropdown must show validator error when untouched after reset',
      );
      expect(postedBodies, hasLength(1),
          reason: 'no stale cityId may reach a second POST');
    },
  );
}