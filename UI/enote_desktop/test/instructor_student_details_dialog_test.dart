import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
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
      ChangeNotifierProvider(
        create: (_) => InstructorStudentProvider(apiClient: apiClient),
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
  });
}
