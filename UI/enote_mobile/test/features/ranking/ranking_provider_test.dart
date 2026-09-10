import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/ranking/ranking_provider.dart';

import '../../helpers.dart';

/// Serves a bare JSON list (the ranking endpoint is unpaged, 02 D6),
/// which [RecordingHttpClient] cannot express (map body only).
class _ListStubClient extends http.BaseClient {
  final List<http.Request> requests = [];
  final Object body;

  _ListStubClient(this.body);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requests.add(request as http.Request);
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

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
    final client = _ListStubClient(_rankingList);
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

  test('getForCourse accepts an items envelope too', () async {
    final client = RecordingHttpClient(
      body: {
        'items': _rankingList,
      },
    );
    final provider = _provider(client);

    final ranking = await provider.getForCourse(2);

    expect(
      client.requests.single.url.path,
      '/api/v1/student/courses/2/ranking',
    );
    expect(ranking, hasLength(2));
  });

  test('getForCourse accepts the live value envelope', () async {
    final client = RecordingHttpClient(
      body: {'value': _rankingList, 'Count': 2},
    );
    final provider = _provider(client);

    final ranking = await provider.getForCourse(2);

    expect(ranking, hasLength(2));
    expect(ranking.first.studentName, 'Student Enote');
  });
}
