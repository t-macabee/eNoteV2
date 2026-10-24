import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/assignment/assignment_form_screen.dart';
import 'package:enote_desktop/features/instructor/assignment/assignment_provider.dart';
import 'package:enote_desktop/widgets/date_time_field.dart';

import 'helpers.dart';

ScriptedClient _client(int statusCode, Map<String, dynamic> body) =>
    ScriptedClient((request) => jsonResponse(body, statusCode));

Future<ScriptedClient> _pumpForm(
  WidgetTester tester, {
  int statusCode = 201,
  Map<String, dynamic> body = const {
    'id': 1,
    'lectureId': 1,
    'title': 'Domaći',
    'description': 'Opis',
    'dueAt': '2026-08-01T10:00:00Z',
  },
}) async {
  tester.view.physicalSize = const Size(1400, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final client = _client(statusCode, body);
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      tokenReader: () => fakeJwt(role: 'Instructor'),
      httpClient: client,
    ),
    httpClient: client,
  );

  await tester.pumpWidget(
    ChangeNotifierProvider<AssignmentProvider>.value(
      value: AssignmentProvider(apiClient: apiClient, lectureId: 1),
      child: const MaterialApp(
        home: AssignmentFormScreen(lectureId: 1),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return client;
}

Future<void> _enterText(WidgetTester tester) async {
  await tester.enterText(find.widgetWithText(TextFormField, 'Naslov'), 'Domaći');
  await tester.enterText(find.widgetWithText(TextFormField, 'Opis'), 'Opis');
}

Future<void> _pickNextMonthDay(WidgetTester tester) async {
  await tester.tap(find.byType(DateTimeField));
  await tester.pumpAndSettle();
  await tester.tap(find.byIcon(Icons.chevron_right));
  await tester.pumpAndSettle();
  await tester.tap(find.text('15'));
  await tester.pumpAndSettle();
  await tester.tap(find.text('OK'));
  await tester.pumpAndSettle();
  await tester.tap(find.text('OK'));
  await tester.pumpAndSettle();
}

Future<void> _pickToday(WidgetTester tester) async {
  await tester.tap(find.byType(DateTimeField));
  await tester.pumpAndSettle();
  await tester.tap(find.text('${DateTime.now().day}'));
  await tester.pumpAndSettle();
  await tester.tap(find.text('OK'));
  await tester.pumpAndSettle();
  await tester.tap(find.text('OK'));
  await tester.pumpAndSettle();
}

Future<void> _fillForm(WidgetTester tester) async {
  await _enterText(tester);
  await _pickNextMonthDay(tester);
}

Finder _dueAtFieldText(String text) => find.descendant(
      of: find.byType(DateTimeField),
      matching: find.text(text),
    );

void main() {
  testWidgets('F1-14: server due date message shows under the date field',
      (tester) async {
    final client = await _pumpForm(tester, statusCode: 400, body: const {
      'status': 400,
      'errors': {
        'DueAt': ['Rok za predaju mora biti u budućnosti.'],
      },
    });

    await _fillForm(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(client.postUrls.length, equals(1));
    expect(_dueAtFieldText('Rok za predaju mora biti u budućnosti.'),
        findsOneWidget);
    expect(find.byType(SnackBar), findsNothing);
  });

  testWidgets('F1-14: a server error without a due date field stays a banner',
      (tester) async {
    final client = await _pumpForm(tester, statusCode: 500, body: const {
      'status': 500,
      'code': 'error.internal',
      'message': 'Greška na serveru.',
    });

    await _fillForm(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(client.postUrls.length, equals(1));
    expect(find.byType(SnackBar), findsOneWidget);
    expect(
      find.descendant(
        of: find.byType(SnackBar),
        matching: find.text('Greška na serveru.'),
      ),
      findsOneWidget,
    );
    expect(_dueAtFieldText('Greška na serveru.'), findsNothing);
  });

  testWidgets('F1-14: a validation error on another field stays a banner',
      (tester) async {
    final client = await _pumpForm(tester, statusCode: 400, body: const {
      'status': 400,
      'errors': {
        'Description': ['Opis je obavezan.'],
      },
    });

    await _fillForm(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(client.postUrls.length, equals(1));
    expect(find.byType(SnackBar), findsOneWidget);
    expect(_dueAtFieldText('Opis je obavezan.'), findsNothing);
  });

  testWidgets('F1-14: a new assignment date cannot start in the past',
      (tester) async {
    await _pumpForm(tester);

    await tester.tap(find.byType(DateTimeField));
    await tester.pumpAndSettle();

    final picker = tester.widget<DatePickerDialog>(find.byType(DatePickerDialog));
    expect(
      DateUtils.dateOnly(picker.firstDate),
      DateUtils.dateOnly(DateTime.now()),
    );
  });

  testWidgets('F1-14: a time earlier today never reaches the server',
      (tester) async {
    final client = await _pumpForm(tester);

    await _enterText(tester);
    await _pickToday(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(client.postUrls, isEmpty);
    expect(_dueAtFieldText('Rok za predaju mora biti u budućnosti.'),
        findsOneWidget);
  });
}
