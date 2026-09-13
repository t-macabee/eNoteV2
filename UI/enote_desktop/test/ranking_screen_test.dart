import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/ranking/ranking_provider.dart';
import 'package:enote_desktop/features/instructor/ranking/ranking_screen.dart';

class _BareListClient extends http.BaseClient {
  final List<http.Request> requests = [];
  final List<dynamic> body;

  _BareListClient(this.body);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requests.add(request as http.Request);
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('RankingView renders the bare unpaged ranking list',
      (tester) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final client = _BareListClient([
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
    ]);
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
