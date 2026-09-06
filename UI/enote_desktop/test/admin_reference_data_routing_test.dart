import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_list_screen.dart';
import 'package:enote_desktop/features/admin/city/city_list_screen.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_list_screen.dart';
import 'package:enote_desktop/features/admin/reference_data/reference_data_dialog.dart';
import 'package:enote_desktop/main.dart';

String _base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

String _fakeJwt({
  String subject = '1',
  String username = 'admin',
  String role = 'Administrator',
}) {
  final header = _base64UrlSegment(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final payload = _base64UrlSegment(jsonEncode({
    'sub': subject,
    'unique_name': username,
    'role': role,
    'exp': DateTime.now()
            .add(const Duration(days: 1))
            .millisecondsSinceEpoch ~/
        1000,
  }));
  return '$header.$payload.signature';
}

class _MockHttpClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final body = jsonEncode({
      'items': [],
      'page': 1,
      'pageSize': 10,
      'totalCount': 0,
    });
    return http.StreamedResponse(
      Stream.value(utf8.encode(body)),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets(
      'Administrator sidebar includes Referentni podaci and routes correctly to tabbed dialog',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = _MockHttpClient();
    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => _fakeJwt(role: 'Administrator'),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: mockClient,
    );

    await tester.pumpWidget(MyApp(authState: authState, apiClient: apiClient));
    await tester.pumpAndSettle();

    // Verify sidebar menu entries: Referentni podaci is present, old separate entries are not
    expect(find.text('Referentni podaci'), findsOneWidget);
    expect(find.text('Gradovi'), findsNothing);
    expect(find.text('Adrese'), findsNothing);
    expect(find.text('Tipovi instrumenata'), findsNothing);

    // Open Reference Data dialog
    await tester.tap(find.text('Referentni podaci'));
    await tester.pumpAndSettle();

    // Verify dialog and tab labels exist
    expect(find.byType(ReferenceDataDialog), findsOneWidget);
    expect(find.text('Gradovi'), findsOneWidget);
    expect(find.text('Adrese'), findsOneWidget);
    expect(find.text('Tipovi instrumenata'), findsOneWidget);
    expect(find.byType(CityListScreen), findsOneWidget);

    // Navigate to Adrese tab
    await tester.tap(find.text('Adrese'));
    await tester.pumpAndSettle();
    expect(find.byType(AddressListScreen), findsOneWidget);

    // Navigate to Tipovi instrumenata tab
    await tester.tap(find.text('Tipovi instrumenata'));
    await tester.pumpAndSettle();
    expect(find.byType(InstrumentTypeListScreen), findsOneWidget);
  });

  testWidgets(
      'Non-administrator sidebar does not include reference data entries',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = _MockHttpClient();
    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => _fakeJwt(role: 'Instructor'),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: mockClient,
    );

    await tester.pumpWidget(MyApp(authState: authState, apiClient: apiClient));
    await tester.pumpAndSettle();

    expect(find.text('Referentni podaci'), findsNothing);
    expect(find.text('Gradovi'), findsNothing);
    expect(find.text('Adrese'), findsNothing);
    expect(find.text('Tipovi instrumenata'), findsNothing);
  });
}
