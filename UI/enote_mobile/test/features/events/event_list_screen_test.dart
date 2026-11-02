import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/events/event_list_screen.dart';
import 'package:enote_mobile/features/events/event_provider.dart';

import '../../helpers.dart';

void main() {
  testWidgets('the to filter is sent as end-of-day', (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode(const {
          'items': [],
          'page': 1,
          'pageSize': 20,
          'totalCount': 0,
        }),
        200,
      ),
    );
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
        child: const MaterialApp(home: Scaffold(body: EventListScreen())),
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
    expect(filtered.single.url.queryParameters['to'], toUtcIso(expected));

    await tester.tap(find.byTooltip('Obriši'));
    await tester.pumpAndSettle();

    expect(find.byTooltip('Obriši'), findsNothing);
    expect(client.requests.last.url.queryParameters.containsKey('to'), isFalse);
  });

  testWidgets('the date fields sit in the list filter row at the list inset', (
    tester,
  ) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode(const {
          'items': [],
          'page': 1,
          'pageSize': 20,
          'totalCount': 0,
        }),
        200,
      ),
    );
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
        child: const MaterialApp(home: Scaffold(body: EventListScreen())),
      ),
    );
    await tester.pumpAndSettle();

    expect(
      tester.getTopLeft(find.widgetWithText(TextFormField, 'Od')).dx,
      AppSpacing.lg,
    );
    expect(find.widgetWithText(TextFormField, 'Do'), findsOneWidget);
  });

  testWidgets('date filter sends from and to in UTC', (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode(const {
          'items': [],
          'page': 1,
          'pageSize': 20,
          'totalCount': 0,
        }),
        200,
      ),
    );
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
        child: const MaterialApp(home: Scaffold(body: EventListScreen())),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(TextFormField, 'Od'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('15'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('OK'));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(TextFormField, 'Do'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('15'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('OK'));
    await tester.pumpAndSettle();

    final now = DateTime.now();
    final expectedFrom = DateTime(now.year, now.month, 15);
    final expectedTo = DateTime(now.year, now.month, 15, 23, 59, 59);

    final filtered = client.requests.where(
      (r) =>
          r.url.queryParameters.containsKey('from') &&
          r.url.queryParameters.containsKey('to'),
    );
    expect(filtered, hasLength(1));

    final from = filtered.single.url.queryParameters['from'];
    final to = filtered.single.url.queryParameters['to'];
    expect(from, endsWith('Z'));
    expect(to, endsWith('Z'));
    expect(from, toUtcIso(expectedFrom));
    expect(to, toUtcIso(expectedTo));
  });
}
