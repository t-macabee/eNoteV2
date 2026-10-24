import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_details_dialog.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';

import 'helpers.dart';

void main() {
  testWidgets('F3-11: failed enrollments fetch shows an inline error',
      (tester) async {
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: ScriptedClient((request) {
        if (request.url.path.endsWith('/membership-payments')) {
          return jsonResponse({
            'items': [
              {
                'id': 7,
                'studentId': 5,
                'amount': 40.0,
                'paidAt': '2026-09-01T00:00:00Z',
                'periodStart': '2026-09-01T00:00:00Z',
                'periodEnd': '2027-01-01T00:00:00Z',
                'receiptNumber': 'CL-7',
              },
            ],
            'page': 1,
            'pageSize': 5,
            'totalCount': 1,
          }, 200);
        }
        if (request.url.path.endsWith('/enrollments')) {
          return jsonResponse({'message': 'boom'}, 500);
        }
        if (request.url.path.endsWith('/lectures')) {
          return jsonResponse({
            'items': [
              {
                'id': 21,
                'studentId': 5,
                'studentName': 'Johann Bach',
                'name': 'Individualni čas',
                'location': 'Učionica 2',
                'lectureType': 'Practical',
                'lectureStatus': 'Scheduled',
                'isCancelled': false,
                'lectureTime': '2026-09-14T18:00:00Z',
                'duration': 45,
                'capacity': 1,
                'attendeeCount': 0,
              },
            ],
            'page': 1,
            'pageSize': 100,
            'totalCount': 1,
          }, 200);
        }
        return jsonResponse({
          'id': 5,
          'appUserId': 50,
          'firstName': 'Johann',
          'lastName': 'Bach',
          'username': 'jbach',
          'membershipPaidUntil': '2027-01-01T00:00:00Z',
        }, 200);
      }),
    );

    final student = StudentDto(
      id: 5,
      appUserId: 50,
      firstName: 'Johann',
      lastName: 'Bach',
      username: 'jbach',
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider(
            create: (_) => InstructorStudentProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider(
            create: (_) => LectureProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: InstructorStudentDetailsDialog(student: student),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Detalji studenta'), findsOneWidget);
    expect(find.text('Greška pri učitavanju upisa.'), findsOneWidget);
    expect(find.text('Nema aktivnih upisa.'), findsNothing);
    expect(find.text('Individualni plan'), findsOneWidget);
    expect(find.text('Individualni čas'), findsOneWidget);
    expect(find.text('Bilješke'), findsOneWidget);
    expect(find.text('Članarina'), findsOneWidget);
    expect(find.textContaining('Potvrda CL-7'), findsOneWidget);
    expect(find.text('Unesi potvrdu'), findsOneWidget);
  });

  testWidgets('E1-10: the warning button is disabled when membership lasts longer than a week',
      (tester) async {
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: ScriptedClient((request) {
        if (request.url.path.endsWith('/membership-payments')) {
          return jsonResponse({
            'items': [],
            'page': 1,
            'pageSize': 5,
            'totalCount': 0,
          }, 200);
        }
        if (request.url.path.endsWith('/enrollments')) {
          return jsonResponse({
            'items': [],
            'page': 1,
            'pageSize': 5,
            'totalCount': 0,
          }, 200);
        }
        if (request.url.path.endsWith('/lectures')) {
          return jsonResponse({
            'items': [],
            'page': 1,
            'pageSize': 100,
            'totalCount': 0,
          }, 200);
        }
        return jsonResponse({
          'id': 5,
          'appUserId': 50,
          'firstName': 'Johann',
          'lastName': 'Bach',
          'username': 'jbach',
          'membershipPaidUntil':
              DateTime.now().add(const Duration(days: 60)).toIso8601String(),
        }, 200);
      }),
    );

    final student = StudentDto(
      id: 5,
      appUserId: 50,
      firstName: 'Johann',
      lastName: 'Bach',
      username: 'jbach',
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider(
            create: (_) => InstructorStudentProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider(
            create: (_) => LectureProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: InstructorStudentDetailsDialog(student: student),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final warningButton = tester.widget<OutlinedButton>(
      find.widgetWithText(OutlinedButton, 'Pošalji upozorenje'),
    );
    expect(warningButton.onPressed, isNull);
    expect(find.text('Nema evidentiranih potvrda.'), findsOneWidget);
  });

  testWidgets('F3-16: enrollments load one page at a time', (tester) async {
    final requestedPages = <String?>[];
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: ScriptedClient((request) {
        if (request.url.path.endsWith('/membership-payments')) {
          return jsonResponse({
            'items': [],
            'page': 1,
            'pageSize': 5,
            'totalCount': 0,
          }, 200);
        }
        if (request.url.path.endsWith('/enrollments')) {
          final page = int.parse(request.url.queryParameters['page'] ?? '1');
          requestedPages.add(request.url.queryParameters['page']);
          final names = page == 1
              ? ['Cello 101', 'Flute 101', 'Guitar 101', 'Piano 101', 'Violin 101']
              : ['Drums 101'];
          return jsonResponse({
            'items': [
              for (final (index, name) in names.indexed)
                {
                  'courseId': page * 100 + index,
                  'courseName': name,
                  'instructorId': 100,
                  'instructorName': 'Ana Novak',
                },
            ],
            'page': page,
            'pageSize': 5,
            'totalCount': 6,
          }, 200);
        }
        if (request.url.path.endsWith('/lectures')) {
          return jsonResponse({
            'items': [],
            'page': 1,
            'pageSize': 100,
            'totalCount': 0,
          }, 200);
        }
        return jsonResponse({
          'id': 5,
          'appUserId': 50,
          'firstName': 'Johann',
          'lastName': 'Bach',
          'username': 'jbach',
          'membershipPaidUntil': '2027-01-01T00:00:00Z',
        }, 200);
      }),
    );

    final student = StudentDto(
      id: 5,
      appUserId: 50,
      firstName: 'Johann',
      lastName: 'Bach',
      username: 'jbach',
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider(
            create: (_) => InstructorStudentProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider(
            create: (_) => LectureProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: InstructorStudentDetailsDialog(student: student),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Cello 101'), findsOneWidget);
    expect(find.text('Drums 101'), findsNothing);
    expect(requestedPages, ['1']);

    await tester.ensureVisible(find.text('Učitaj još'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Učitaj još'));
    await tester.pumpAndSettle();

    expect(requestedPages, ['1', '2']);
    expect(find.text('Drums 101'), findsOneWidget);
    expect(find.text('Učitaj još'), findsNothing);
  });
}
