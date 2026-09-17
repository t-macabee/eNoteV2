import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_list_screen.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';

class _CountingClient extends http.BaseClient {
  int fetches = 0;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    fetches++;
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode({
        'items': [],
        'page': 1,
        'pageSize': 20,
        'totalCount': 0,
      }))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('F3-10: cancelling the student create form does not refresh',
      (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final client = _CountingClient();
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: client,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider(
            create: (_) => InstructorStudentProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(
          home: Scaffold(body: InstructorStudentListScreen()),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(client.fetches, 1);

    await tester.tap(find.text('Kreiraj studenta'));
    await tester.pumpAndSettle();
    expect(find.text('Otkaži'), findsOneWidget);

    await tester.tap(find.text('Otkaži'));
    await tester.pumpAndSettle();

    expect(client.fetches, 1);
  });
}
