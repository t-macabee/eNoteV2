import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_form_screen.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';

/// Records every request body sent through it and answers each POST with a
/// minimal valid CourseDto JSON payload, so [CourseProvider.insert] succeeds
/// without a real backend.
class _RecordingHttpClient extends http.BaseClient {
  final List<String?> postedBodies = [];
  int _nextId = 1;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final body = request is http.Request ? request.body : null;
    postedBodies.add(body);

    final responseJson = jsonEncode({
      'id': _nextId++,
      'instructorId': 1,
      'name': 'Kurs',
      'isPublished': false,
      'price': 10.0,
      'enrolledCount': 0,
    });
    final bytes = utf8.encode(responseJson);
    return http.StreamedResponse(
      Stream.value(bytes),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets(
    'course add form pops true after a successful add (auto-close)',
    (WidgetTester tester) async {
      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final httpClient = _RecordingHttpClient();
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: httpClient,
      );
      final courseProvider = CourseProvider(apiClient: apiClient);

      bool? dialogResult;

      await tester.pumpWidget(
        ChangeNotifierProvider<CourseProvider>.value(
          value: courseProvider,
          child: MaterialApp(
            home: Scaffold(
              body: Builder(
                builder: (context) => ElevatedButton(
                  onPressed: () async {
                    dialogResult = await EntityFormScaffold.showAsDialog(
                      context,
                      builder: (_) => const CourseFormScreen(
                        presentation: EntityFormPresentation.dialog,
                      ),
                    );
                  },
                  child: const Text('Open'),
                ),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      expect(find.text('Dodaj kurs'), findsOneWidget);

      await tester.enterText(
          find.widgetWithText(TextFormField, 'Naziv'), 'Test kurs');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'Cijena'), '10');

      await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
      await tester.pumpAndSettle();

      expect(httpClient.postedBodies, hasLength(1));
      // The add form auto-closes on success instead of resetting in place.
      expect(find.text('Dodaj kurs'), findsNothing);
      expect(dialogResult, isTrue);
    },
  );
}
