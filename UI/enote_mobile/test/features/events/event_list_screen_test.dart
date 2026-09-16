import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/events/event_list_screen.dart';
import 'package:enote_mobile/features/events/event_provider.dart';

import '../../helpers.dart';

class _EventStubClient extends http.BaseClient {
  final List<http.Request> requests = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request is http.Request) {
      requests.add(request);
    }
    const body = {'items': [], 'page': 1, 'pageSize': 20, 'totalCount': 0};
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('the to filter is sent as end-of-day', (tester) async {
    final client = _EventStubClient();
    final authState = AuthState(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      httpClient: client,
    );
    final apiClient = ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthState>.value(value: authState),
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<EventProvider>(
            create: (_) => EventProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(
          home: Scaffold(body: EventListScreen()),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(TextFormField, 'Do'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('15'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('OK'));
    await tester.pumpAndSettle();

    final filtered = client.requests.where(
      (r) => r.url.queryParameters.containsKey('to'),
    );
    expect(filtered, hasLength(1));

    final now = DateTime.now();
    final expected = DateTime(now.year, now.month, 15, 23, 59, 59);
    expect(
      filtered.single.url.queryParameters['to'],
      expected.toIso8601String(),
    );
  });
}
