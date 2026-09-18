import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_list_screen.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';

import 'helpers.dart';

void main() {
  testWidgets('F3-10: cancelling the student create form does not refresh',
      (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final client = ScriptedClient(
      (_) => jsonResponse(const {
        'items': [],
        'page': 1,
        'pageSize': 20,
        'totalCount': 0,
      }, 200),
    );
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
    expect(client.requests, hasLength(1));

    await tester.tap(find.text('Kreiraj studenta'));
    await tester.pumpAndSettle();
    expect(find.text('Otkaži'), findsOneWidget);

    await tester.tap(find.text('Otkaži'));
    await tester.pumpAndSettle();

    expect(client.requests, hasLength(1));
  });
}
