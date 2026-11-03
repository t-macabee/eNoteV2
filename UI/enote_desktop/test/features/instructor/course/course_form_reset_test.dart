import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_form_screen.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';

import '../../../helpers.dart';

void main() {
  testWidgets('course add form pops true after a successful add (auto-close)', (
    WidgetTester tester,
  ) async {
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
      find.widgetWithText(TextFormField, 'Naziv'),
      'Test kurs',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Mjesečna cijena'),
      '10',
    );

    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(postedBodies, hasLength(1));
    final posted = jsonDecode(postedBodies.single!) as Map<String, dynamic>;
    expect(posted['isPublished'], isFalse);
    expect(find.text('Dodaj kurs'), findsNothing);
    expect(dialogResult, isTrue);
  });

  testWidgets('empty price shows the course price message', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: ScriptedClient((_) => jsonResponse('', 200)),
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<CourseProvider>(
        create: (_) => CourseProvider(apiClient: apiClient),
        child: const MaterialApp(home: CourseFormScreen()),
      ),
    );
    await tester.pumpAndSettle();

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Naziv'),
      'Test kurs',
    );

    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(
      find.text(
        'Unesite mjesečnu cijenu kursa (npr. 25.00). Cijena 0 znači '
        'besplatan kurs.',
      ),
      findsOneWidget,
    );
  });

  testWidgets('course form shows its sections and saves a valid form once', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final postedBodies = <String?>[];
    final httpClient = ScriptedClient((request) {
      postedBodies.add(request.body);
      return jsonResponse({
        'id': 1,
        'instructorId': 1,
        'name': 'Kurs',
        'isPublished': false,
        'price': 10.0,
        'enrolledCount': 0,
      }, 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: httpClient,
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<CourseProvider>(
        create: (_) => CourseProvider(apiClient: apiClient),
        child: const MaterialApp(home: CourseFormScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Osnovni podaci'), findsOneWidget);
    expect(find.text('Trajanje'), findsOneWidget);

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Naziv'),
      'Test kurs',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Mjesečna cijena'),
      '10',
    );

    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(postedBodies, hasLength(1));
  });
}
