import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/assignments/assignment_list_screen.dart';
import 'package:enote_mobile/features/assignments/assignment_provider.dart';
import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/shell/routes.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

Map<String, dynamic> _assignment({
  required int id,
  required String title,
  String? submittedAt,
  int? grade,
}) => {
  'id': id,
  'lectureId': 11,
  'title': title,
  'description': 'Opis',
  'dueAt': '2026-12-17T23:59:00',
  'mySubmittedAt': ?submittedAt,
  'myGrade': ?grade,
};

Future<ScriptedClient> _pumpAssignments(
  WidgetTester tester, {
  required List<Map<String, dynamic>> items,
  int openCount = 3,
  AssignmentFilter initialFilter = AssignmentFilter.open,
  bool asRoute = false,
}) async {
  setPhoneSurface(tester);
  final client = ScriptedClient((request) {
    final isCount = request.url.queryParameters['pageSize'] == '1';
    return jsonResponse(
      jsonEncode({
        'items': isCount ? [] : items,
        'page': 1,
        'pageSize': isCount ? 1 : 20,
        'totalCount': isCount ? openCount : items.length,
      }),
      200,
    );
  });
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

  final providers = MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthState>.value(value: authState),
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<AssignmentProvider>(
        create: (_) => AssignmentProvider(apiClient: apiClient),
      ),
    ],
    child: asRoute
        ? MaterialApp(
            initialRoute: AppRoutes.assignmentHistory,
            onGenerateRoute: AppRouter.onGenerateRoute,
          )
        : MaterialApp(
            home: Scaffold(
              body: AssignmentListScreen(initialFilter: initialFilter),
            ),
          ),
  );

  await tester.pumpWidget(providers);
  await tester.pumpAndSettle();
  return client;
}

void main() {
  testWidgets('opens on Otvoreni with isSubmitted false and dueAfter', (
    tester,
  ) async {
    final client = await _pumpAssignments(tester, items: const []);

    final otvoreni = tester.widget<ChoiceChip>(
      find.widgetWithText(ChoiceChip, 'Otvoreni (3)'),
    );
    expect(otvoreni.selected, isTrue);

    final listRequest = client.requests.firstWhere(
      (r) => r.url.queryParameters['pageSize'] == '20',
    );
    expect(listRequest.url.queryParameters['isSubmitted'], 'false');
    expect(listRequest.url.queryParameters, contains('dueAfter'));
    expect(listRequest.url.queryParameters, isNot(contains('dueBefore')));
  });

  testWidgets('the Otvoreni tab shows the open count', (tester) async {
    await _pumpAssignments(tester, items: const [], openCount: 5);

    expect(find.text('Otvoreni (5)'), findsOneWidget);
  });

  testWidgets('a graded assignment row shows its grade', (tester) async {
    await _pumpAssignments(
      tester,
      items: [
        _assignment(
          id: 1,
          title: 'Ljestvica C-dur',
          submittedAt: '2026-11-15T18:20:00',
          grade: 85,
        ),
      ],
    );

    expect(find.text('Ocjena: 85'), findsOneWidget);
  });

  testWidgets('a submitted assignment row shows Predano', (tester) async {
    await _pumpAssignments(
      tester,
      items: [
        _assignment(id: 2, title: 'Akordi', submittedAt: '2026-11-15T18:20:00'),
      ],
    );

    expect(find.text('Predano'), findsOneWidget);
  });

  testWidgets('assignment history route opens on Predani', (tester) async {
    final client = await _pumpAssignments(
      tester,
      items: const [],
      asRoute: true,
    );

    expect(find.text('Predani zadaci'), findsOneWidget);
    final predani = tester.widget<ChoiceChip>(
      find.widgetWithText(ChoiceChip, 'Predani'),
    );
    expect(predani.selected, isTrue);

    final listRequest = client.requests.firstWhere(
      (r) => r.url.queryParameters['pageSize'] == '20',
    );
    expect(listRequest.url.queryParameters['isSubmitted'], 'true');
  });
}
