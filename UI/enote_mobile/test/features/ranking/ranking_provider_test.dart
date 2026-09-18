import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/ranking/ranking_provider.dart';

import '../../helpers.dart';

const _rankingList = [
  {
    'rank': 1,
    'studentId': 7,
    'studentName': 'Student Enote',
    'averageGrade': 9.5,
    'gradedSubmissions': 3,
  },
  {
    'rank': 2,
    'studentId': 9,
    'studentName': 'Drugi Student',
    'averageGrade': null,
    'gradedSubmissions': 0,
  },
];

RankingProvider _provider(http.Client client) {
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return RankingProvider(
    apiClient: ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    ),
  );
}

void main() {
  test('getForCourse issues GET student/courses/{id}/ranking', () async {
    final client = ScriptedClient(
      (_) => jsonResponse(jsonEncode(_rankingList), 200),
    );
    final provider = _provider(client);

    final ranking = await provider.getForCourse(2);

    expect(client.requests, hasLength(1));
    expect(client.requests.single.method, 'GET');
    expect(
      client.requests.single.url.path,
      '/api/v1/student/courses/2/ranking',
    );
    expect(ranking, hasLength(2));
    expect(ranking.first.rank, 1);
    expect(ranking.first.studentName, 'Student Enote');
    expect(ranking.first.averageGrade, 9.5);
    expect(ranking.last.averageGrade, isNull);
  });
}
