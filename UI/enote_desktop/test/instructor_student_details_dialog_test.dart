import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_details_dialog.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';

class _FailingEnrollmentsClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request.url.path.endsWith('/enrollments')) {
      return http.StreamedResponse(
        Stream.value(utf8.encode('{"message":"boom"}')),
        500,
        headers: {'content-type': 'application/json'},
      );
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode({
        'id': 5,
        'appUserId': 50,
        'firstName': 'Johann',
        'lastName': 'Bach',
        'username': 'jbach',
        'membershipPaidUntil': '2027-01-01T00:00:00Z',
      }))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('F3-11: failed enrollments fetch shows an inline error',
      (tester) async {
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: _FailingEnrollmentsClient(),
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
