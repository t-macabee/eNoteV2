import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/ranking/ranking_provider.dart';
import 'package:enote_desktop/features/instructor/ranking/ranking_screen.dart';

import 'helpers.dart';

void main() {
  testWidgets('RankingView renders the ranking list',
      (tester) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final client = ScriptedClient(
      (_) => jsonResponse([
        {
          'rank': 1,
          'studentId': 7,
          'studentName': 'Student Enote',
          'averageGrade': 9.5,
          'gradedSubmissions': 3,
        },
        {
          'rank': 2,
          'studentId': 9,
          'studentName': 'Drugi Student',
          'averageGrade': null,
          'gradedSubmissions': 0,
        },
      ], 200),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: client,
    );

    await tester.pumpWidget(
      Provider<RankingProvider>.value(
        value: RankingProvider(apiClient: apiClient),
        child: const MaterialApp(
          home: Scaffold(body: RankingView(courseId: 2)),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(
      client.requests.single.url.path,
      '/api/v1/instructor/courses/2/ranking',
    );
    expect(find.text('Student Enote'), findsOneWidget);
    expect(find.text('Drugi Student'), findsOneWidget);
    expect(find.textContaining('Prosjek: 9.50'), findsOneWidget);
    expect(find.textContaining('Prosjek: -'), findsOneWidget);
  });
}
