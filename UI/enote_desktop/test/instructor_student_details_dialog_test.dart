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
  });
}
