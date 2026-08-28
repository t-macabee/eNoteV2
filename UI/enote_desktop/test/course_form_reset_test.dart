import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_form_screen.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';

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
    'saving the course form twice in a row does not resend the first '
    "save's dates on the second, untouched submission",
    (WidgetTester tester) async {
      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final httpClient = _RecordingHttpClient();
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: httpClient,
      );
      final courseProvider = CourseProvider(apiClient: apiClient);

      await tester.pumpWidget(
        ChangeNotifierProvider<CourseProvider>.value(
          value: courseProvider,
          child: const MaterialApp(home: CourseFormScreen()),
        ),
      );

      Future<void> fillRequiredTextFields() async {
        await tester.enterText(find.widgetWithText(TextFormField, 'Naziv'), 'Test kurs');
        await tester.enterText(find.widgetWithText(TextFormField, 'Cijena'), '10');
      }

      Future<void> pickDate(int calendarIconIndex) async {
        await tester.tap(find.byIcon(Icons.calendar_today).at(calendarIconIndex));
        await tester.pumpAndSettle();
        await tester.tap(find.text('OK'));
        await tester.pumpAndSettle();
      }

      Future<void> save() async {
        await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
        await tester.pumpAndSettle();
      }

      // First save: fill both dates.
      await fillRequiredTextFields();
      await pickDate(0); // Datum početka
      await pickDate(1); // Datum završetka
      await save();

      expect(httpClient.postedBodies, hasLength(1));
      final firstBody = jsonDecode(httpClient.postedBodies[0]!) as Map<String, dynamic>;
      expect(firstBody['startDate'], isNotNull);
      expect(firstBody['endDate'], isNotNull);

      // Second save: only re-fill the text fields, exactly like the repro —
      // the date fields are left untouched.
      await fillRequiredTextFields();
      await save();

      expect(httpClient.postedBodies, hasLength(2));
      final secondBody = jsonDecode(httpClient.postedBodies[1]!) as Map<String, dynamic>;
      expect(secondBody.containsKey('startDate'), isFalse,
          reason: 'startDate from the first save leaked into the second POST body');
      expect(secondBody.containsKey('endDate'), isFalse,
          reason: 'endDate from the first save leaked into the second POST body');
    },
  );
}
