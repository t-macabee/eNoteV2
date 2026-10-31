import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/student_directory/student_directory_provider.dart';
import 'package:enote_desktop/features/instructor/student_directory/student_directory_screen.dart';

import '../../../helpers.dart';

const _studentsPage = {
  'items': [
    {
      'id': 5,
      'appUserId': 70,
      'firstName': 'Johann',
      'lastName': 'Bach',
      'username': 'jbach',
    },
  ],
  'page': 1,
  'pageSize': 24,
  'totalCount': 1,
};

Future<void> _pumpScreen(WidgetTester tester, ScriptedClient client) async {
  tester.view.physicalSize = const Size(1400, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(),
    httpClient: client,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        Provider<StudentDirectoryProvider>(
          create: (_) => StudentDirectoryProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(
        home: Scaffold(body: StudentDirectoryScreen()),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('the directory sends the name query parameter after a search',
      (tester) async {
    final client = ScriptedClient((_) => jsonResponse(_studentsPage, 200));
    await _pumpScreen(tester, client);

    final searchField = find.widgetWithText(
      TextField,
      'Pretraži sve studente po imenu ili korisničkom imenu...',
    );
    await tester.enterText(searchField, 'bach');
    await pumpPastDebounce(tester);
    await tester.pumpAndSettle();

    final searchUrl = client.getUrls.last;
    expect(searchUrl, contains('instructor/student-directory'));
    expect(searchUrl, contains('name=bach'));
  });

  testWidgets('a tap on a row opens the profile dialog with course grades',
      (tester) async {
    final client = ScriptedClient((request) {
      if (request.url.path.endsWith('/5')) {
        return jsonResponse(const {
          'id': 5,
          'appUserId': 70,
          'firstName': 'Johann',
          'lastName': 'Bach',
          'username': 'jbach',
          'courses': [
            {
              'courseId': 1,
              'courseName': 'Guitar 101',
              'instructorName': 'Jane Doe',
              'enrollmentStatus': 'Active',
              'averageGrade': 4.5,
              'gradedSubmissions': 2,
            },
            {
              'courseId': 2,
              'courseName': 'Piano 101',
              'instructorName': 'John Doe',
              'enrollmentStatus': 'Completed',
              'averageGrade': null,
              'gradedSubmissions': 0,
            },
          ],
        }, 200);
      }
      return jsonResponse(_studentsPage, 200);
    });
    await _pumpScreen(tester, client);

    await tester.tap(find.text('Johann Bach'));
    await tester.pumpAndSettle();

    expect(client.getUrls.last, contains('instructor/student-directory/5'));
    expect(find.text('Guitar 101'), findsOneWidget);
    expect(find.text('Piano 101'), findsOneWidget);
    expect(find.text('4.50'), findsOneWidget);
    expect(find.text('—'), findsOneWidget);
  });

  testWidgets('an empty course list shows the empty message', (tester) async {
    final client = ScriptedClient((request) {
      if (request.url.path.endsWith('/5')) {
        return jsonResponse(const {
          'id': 5,
          'appUserId': 70,
          'firstName': 'Johann',
          'lastName': 'Bach',
          'username': 'jbach',
          'courses': [],
        }, 200);
      }
      return jsonResponse(_studentsPage, 200);
    });
    await _pumpScreen(tester, client);

    await tester.tap(find.text('Johann Bach'));
    await tester.pumpAndSettle();

    expect(find.text('Student nema kurseva.'), findsOneWidget);
  });
}
