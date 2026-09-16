import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/assignment_submission/submission_list_screen.dart';
import 'package:enote_desktop/features/instructor/assignment_submission/submission_provider.dart';

import 'helpers.dart';

class FakeClient extends http.BaseClient {
  final Future<http.StreamedResponse> Function(http.BaseRequest request) handler;

  FakeClient(this.handler);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) => handler(request);
}

void main() {
  testWidgets('SubmissionListScreen shows no search bar', (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final client = FakeClient((request) async {
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode({
          'items': [],
          'page': 1,
          'pageSize': 20,
          'totalCount': 0,
        }))),
        200,
      );
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

    expect(find.byType(TextField), findsNothing);
  });
}
