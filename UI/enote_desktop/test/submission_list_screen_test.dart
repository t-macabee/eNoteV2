import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/assignment_submission/submission_list_screen.dart';
import 'package:enote_desktop/features/instructor/assignment_submission/submission_provider.dart';

import 'helpers.dart';

Future<ScriptedClient> _pumpList(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1400, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final client = ScriptedClient((request) {
    return jsonResponse({
      'items': [],
      'page': 1,
      'pageSize': 20,
      'totalCount': 0,
    }, 200);
  });

  final authState = AuthState(
    baseUrl: 'http://localhost/',
    httpClient: client,
    tokenReader: () => fakeJwt(username: 'test', role: 'Instructor'),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost/',
    authState: authState,
    httpClient: client,
  );
  final provider = SubmissionProvider(
    apiClient: apiClient,
    lectureId: 1,
    assignmentId: 2,
  );

  await tester.pumpWidget(MaterialApp(
    home: ChangeNotifierProvider<SubmissionProvider>.value(
      value: provider,
      child: const SubmissionListScreen(
        lectureId: 1,
        assignmentId: 2,
        assignmentTitle: 'Test zadatak',
      ),
    ),
  ));

  await tester.pumpAndSettle();
  return client;
}

void main() {
  testWidgets('SubmissionListScreen shows the student name search and grade filter', (tester) async {
    await _pumpList(tester);

    expect(find.byType(TextField), findsOneWidget);
    expect(find.text('Pretraži po imenu studenta...'), findsOneWidget);
    expect(find.byType(DropdownButtonFormField<bool?>), findsOneWidget);
  });

  testWidgets('SubmissionListScreen sends the student name and graded filter', (tester) async {
    final client = await _pumpList(tester);

    await tester.tap(find.byType(DropdownButtonFormField<bool?>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Neocijenjeno').last);
    await tester.pumpAndSettle();

    expect(
      Uri.parse(client.getUrls.last).queryParameters['isGraded'],
      'false',
    );

    await tester.enterText(find.byType(TextField), 'Jana');
    await pumpPastDebounce(tester);
    await tester.pumpAndSettle();

    final query = Uri.parse(client.getUrls.last).queryParameters;
    expect(query['studentName'], 'Jana');
    expect(query['isGraded'], 'false');
  });
}
