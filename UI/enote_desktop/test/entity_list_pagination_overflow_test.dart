import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/city/city_list_screen.dart';
import 'package:enote_desktop/features/admin/city/city_provider.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';

class _CityHttpClient extends http.BaseClient {
  final List<Map<String, dynamic>> items;
  final int totalCount;

  _CityHttpClient({
    this.items = const [
      {'id': 1, 'name': 'Sarajevo'},
      {'id': 2, 'name': 'Mostar'},
    ],
    this.totalCount = 2,
  });

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final body = jsonEncode({
      'items': items,
      'page': 1,
      'pageSize': 10,
      'totalCount': totalCount,
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
      'CityListScreen pagination row does not overflow at wide, uncapped widths',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final httpClient = _CityHttpClient();
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: httpClient,
    );
    final cityProvider = CityProvider(apiClient: apiClient);

    await tester.pumpWidget(
      ChangeNotifierProvider<CityProvider>.value(
        value: cityProvider,
        child: const MaterialApp(
          home: Scaffold(
            body: CityListScreen(
              presentation: EntityListPresentation.embedded,
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.textContaining('Stranica'), findsOneWidget);
  });

  testWidgets(
      'CityListScreen renders Stranica 1 od 1 on empty result set',
      (WidgetTester tester) async {
    final httpClient = _CityHttpClient(items: [], totalCount: 0);
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: httpClient,
    );
    final cityProvider = CityProvider(apiClient: apiClient);

    await tester.pumpWidget(
      ChangeNotifierProvider<CityProvider>.value(
        value: cityProvider,
        child: const MaterialApp(
          home: Scaffold(
            body: CityListScreen(
              presentation: EntityListPresentation.embedded,
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('Stranica 1 od 1'), findsOneWidget);
  });
}
