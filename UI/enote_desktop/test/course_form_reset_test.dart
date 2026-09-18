import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_form_screen.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';

import 'helpers.dart';

void main() {
  testWidgets(
    'course add form pops true after a successful add (auto-close)',
    (WidgetTester tester) async {
      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final postedBodies = <String?>[];
      var nextId = 1;
      final httpClient = ScriptedClient((request) {
        postedBodies.add(request.body);
        return jsonResponse({
          'id': nextId++,
          'instructorId': 1,
          'name': 'Kurs',
          'isPublished': false,
          'price': 10.0,
          'enrolledCount': 0,
        }, 200);
      });
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
          find.widgetWithText(TextFormField, 'Mjesečna cijena'), '10');

      await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
      await tester.pumpAndSettle();

      expect(postedBodies, hasLength(1));
      // F3-07: create payload carries isPublished false (publishing lives in
      // the detail dialog behind its confirm).
      final posted = jsonDecode(postedBodies.single!) as Map<String, dynamic>;
      expect(posted['isPublished'], isFalse);
      // The add form auto-closes on success instead of resetting in place.
      expect(find.text('Dodaj kurs'), findsNothing);
      expect(dialogResult, isTrue);
    },
  );
}
