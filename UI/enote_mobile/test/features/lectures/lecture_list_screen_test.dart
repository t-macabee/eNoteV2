import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/lectures/lecture_list_screen.dart';
import 'package:enote_mobile/features/lectures/lecture_provider.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

Future<ScriptedClient> _pumpLectures(
  WidgetTester tester, {
  int upcomingCount = 7,
}) async {
  setPhoneSurface(tester);
  final client = ScriptedClient(
    (request) => jsonResponse(
      jsonEncode({
        'items': [],
        'page': 1,
        'pageSize': int.parse(request.url.queryParameters['pageSize'] ?? '20'),
        'totalCount': upcomingCount,
      }),
      200,
    ),
  );
  final authState = AuthState(
    baseUrl: _baseUrl,
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  final apiClient = ApiClient(
    baseUrl: _baseUrl,
    authState: authState,
    httpClient: client,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<LectureProvider>(
          create: (_) => LectureProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(home: Scaffold(body: LectureListScreen())),
    ),
  );
  await tester.pumpAndSettle();
  return client;
}

void main() {
  testWidgets('opens on Predstojeća with from and shows the count', (
    tester,
  ) async {
    final client = await _pumpLectures(tester);

    final upcoming = tester.widget<ChoiceChip>(
      find.widgetWithText(ChoiceChip, 'Predstojeća (7)'),
    );
    expect(upcoming.selected, isTrue);

    final listRequest = client.requests.firstWhere(
      (r) => r.url.queryParameters['pageSize'] == '20',
    );
    expect(listRequest.url.queryParameters, contains('from'));
  });

  testWidgets('the Sva tab sends no from', (tester) async {
    final client = await _pumpLectures(tester);

    await tester.tap(find.text('Sva'));
    await tester.pumpAndSettle();

    expect(client.requests.last.url.queryParameters, isNot(contains('from')));
  });
}
