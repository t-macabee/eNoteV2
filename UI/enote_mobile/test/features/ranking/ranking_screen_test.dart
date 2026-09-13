import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/ranking/ranking_provider.dart';
import 'package:enote_mobile/features/ranking/ranking_screen.dart';
import 'package:enote_mobile/session/session_controller.dart';

import '../../helpers.dart';

class _RetryStubClient extends http.BaseClient {
  int attempts = 0;
  final String successJson;

  _RetryStubClient({required this.successJson});

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    attempts++;
    if (attempts == 1) {
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode({'detail': 'Server error'}))),
        500,
        headers: {'content-type': 'application/problem+json'},
      );
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(successJson)),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('RankingScreen retries fetch on button tap after error',
      (WidgetTester tester) async {
    final rankingJson = jsonEncode([
      {
        'studentId': 7,
        'studentName': 'Amir Hadzic',
        'rank': 1,
        'averageGrade': 9.5,
        'gradedSubmissions': 4,
      },
    ]);

    final client = _RetryStubClient(successJson: rankingJson);
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
    final rankingProvider = RankingProvider(apiClient: apiClient);
    final sessionController = SessionController(
      apiClient: apiClient,
      authState: authState,
      notifications: NotificationController(
        apiClient: apiClient,
        endpoint: 'student/notifications',
      ),
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          Provider<RankingProvider>.value(value: rankingProvider),
          ChangeNotifierProvider<SessionController>.value(
            value: sessionController,
          ),
        ],
        child: const MaterialApp(
          home: RankingScreen(courseId: 2),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Pokušaj ponovo'), findsOneWidget);
    expect(find.text('Amir Hadzic'), findsNothing);

    await tester.tap(find.text('Pokušaj ponovo'));
    await tester.pumpAndSettle();

    expect(find.text('Amir Hadzic'), findsOneWidget);
    expect(find.text('1'), findsOneWidget);
  });
}
