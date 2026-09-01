import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/address/address_form_screen.dart';
import 'package:enote_desktop/features/admin/city/city_provider.dart';

/// Records every request body sent through it and answers each POST with a
/// minimal valid AddressReferenceDto JSON payload, so [AddressProvider.insert]
/// succeeds without a real backend.
class _AddressRecordingHttpClient extends http.BaseClient {
  final List<String?> postedBodies = [];
  int _nextId = 1;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request.method == 'GET') {
      final responseJson = jsonEncode({
        'items': [
          {'id': 1, 'name': 'Sarajevo'},
          {'id': 2, 'name': 'Mostar'},
        ],
        'page': 1,
        'pageSize': 100,
        'totalCount': 2,
      });
      final bytes = utf8.encode(responseJson);
      return http.StreamedResponse(
        Stream.value(bytes),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    final body = request is http.Request ? request.body : null;
    postedBodies.add(body);

    final responseJson = jsonEncode({
      'id': _nextId++,
      'cityId': 1,
      'city': 'Sarajevo',
      'street': 'Test ulica',
      'number': '1',
    });
    final bytes = utf8.encode(responseJson);
    return http.StreamedResponse(
      Stream.value(bytes),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets(
    'after reset the city dropdown visually clears and '
    'required field validator blocks submission without a stale value',
    (WidgetTester tester) async {
      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final httpClient = _AddressRecordingHttpClient();
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

      expect(httpClient.postedBodies, hasLength(1));
      final firstBody = jsonDecode(httpClient.postedBodies[0]!) as Map<String, dynamic>;
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
      expect(httpClient.postedBodies, hasLength(1),
          reason: 'no stale cityId may reach a second POST');
    },
  );
}