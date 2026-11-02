import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/home/home_provider.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

void main() {
  test('load calls student/home and parses the DTO', () async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode({
          'nextLecture': {
            'id': 11,
            'name': 'Akordi I',
            'location': 'Sala 2',
            'lectureType': 'Theoretical',
            'lectureStatus': 'Scheduled',
            'isCancelled': false,
            'lectureTime': '2026-12-17T10:00:00',
            'duration': 60,
            'capacity': 20,
            'attendeeCount': 5,
            'myAttendanceStatus': 'Pending',
          },
          'openAssignments': [
            {
              'id': 3,
              'lectureId': 11,
              'title': 'Ljestvica C-dur',
              'description': 'Opis',
              'dueAt': '2026-12-18T23:59:00',
            },
          ],
          'currentRentals': [],
          'tuitionDueCourses': [],
          'debt': {'hasUnpaidDebt': true, 'rentalId': 5},
        }),
        200,
      ),
    );
    final authState = AuthState(
      baseUrl: _baseUrl,
      tokenReader: () => fakeJwt(),
      httpClient: client,
    );
    final apiClient = ApiClient(
      baseUrl: _baseUrl,
      authState: authState,
      httpClient: client,
    );

    final home = await HomeProvider(apiClient: apiClient).load();

    expect(client.requests.single.url.path, '/api/v1/student/home');
    expect(home.nextLecture?.name, 'Akordi I');
    expect(home.openAssignments.single.title, 'Ljestvica C-dur');
    expect(home.debt.hasUnpaidDebt, isTrue);
    expect(home.debt.rentalId, 5);
  });
}
