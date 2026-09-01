import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/shop_instrument_type_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_form_screen.dart';

/// Records every request body sent through it and answers each POST with a
/// minimal valid InstrumentDto JSON payload, so [InstrumentProvider.insert]
/// succeeds without a real backend.
class _InstrumentRecordingHttpClient extends http.BaseClient {
  final List<String?> postedBodies = [];
  int _nextId = 1;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request.method == 'GET') {
      final responseJson = jsonEncode({
        'items': [
          {'id': 1, 'type': 'Gitara'},
          {'id': 2, 'type': 'Klavir'},
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
      'model': 'Test model',
      'manufacturer': 'Test proizvođač',
      'description': 'Test opis',
      'instrumentTypeId': 1,
      'instrumentType': 'Gitara',
      'musicStore': 'Trgovina A',
      'isAvailable': true,
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
    'after reset the instrument type dropdown visually clears and '
    'required field validator blocks submission without a stale value',
    (WidgetTester tester) async {
      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final httpClient = _InstrumentRecordingHttpClient();
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

      expect(httpClient.postedBodies, hasLength(1));
      final firstBody =
          jsonDecode(httpClient.postedBodies[0]!) as Map<String, dynamic>;
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
      expect(httpClient.postedBodies, hasLength(1),
          reason: 'no stale instrumentTypeId may reach a second POST');
    },
  );
}