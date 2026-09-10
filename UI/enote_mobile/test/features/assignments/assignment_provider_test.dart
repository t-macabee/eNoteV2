import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/assignments/assignment_provider.dart';

import '../../helpers.dart';

const _assignmentJson = {
  'id': 5,
  'lectureId': 11,
  'title': 'Akordi — vježba 1',
  'description': 'Snimite sebe kako svirate akorde.',
  'dueAt': '2026-09-17T23:59:00',
};

const _submissionJson = {
  'id': 3,
  'assignmentId': 5,
  'studentId': 7,
  'studentName': 'Student Enote',
  'filePath': '/api/v1/uploads/assignments/abc.pdf',
  'submittedAt': '2026-09-15T18:20:00',
  'grade': null,
};

const _pageBody = {
  'items': [_assignmentJson],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

const _historyBody = {
  'items': [_submissionJson],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

/// Serves multipart requests, which [RecordingHttpClient] cannot express
/// (it casts every request to [http.Request]).
class _MultipartStubClient extends http.BaseClient {
  final List<http.BaseRequest> sent = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    sent.add(request);
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(_submissionJson))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

AssignmentProvider _provider(http.Client client) {
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return AssignmentProvider(
    apiClient: ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    ),
  );
}

void main() {
  test('getPage issues GET student/assignments with the filter query',
      () async {
    final client = RecordingHttpClient(body: _pageBody);
    final provider = _provider(client);

    final page = await provider.getPage(
      params: {
        'page': 1,
        'pageSize': 20,
        'includeTotalCount': true,
        'title': 'akordi',
        'dueAfter': '2026-09-01T00:00:00.000',
        'dueBefore': '2026-09-30T00:00:00.000',
      },
    );

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'GET');
    expect(sent.url.path, '/api/v1/student/assignments');
    expect(sent.url.queryParameters['page'], '1');
    expect(sent.url.queryParameters['pageSize'], '20');
    expect(sent.url.queryParameters['includeTotalCount'], 'true');
    expect(sent.url.queryParameters['title'], 'akordi');
    expect(sent.url.queryParameters['dueAfter'], '2026-09-01T00:00:00.000');
    expect(sent.url.queryParameters['dueBefore'], '2026-09-30T00:00:00.000');
    expect(page.items, hasLength(1));
    expect(page.items.single.title, 'Akordi — vježba 1');
    expect(page.items.single.lectureId, 11);
  });

  test('getById issues GET student/assignments/{id}', () async {
    final client = RecordingHttpClient(body: _assignmentJson);
    final provider = _provider(client);

    final assignment = await provider.getById(5);

    expect(client.requests.single.url.path, '/api/v1/student/assignments/5');
    expect(assignment.title, 'Akordi — vježba 1');
  });

  test('mySubmission issues GET student/assignments/{id}/submission',
      () async {
    final client = RecordingHttpClient(body: _submissionJson);
    final provider = _provider(client);

    final submission = await provider.mySubmission(5);

    expect(client.requests, hasLength(1));
    expect(client.requests.single.method, 'GET');
    expect(
      client.requests.single.url.path,
      '/api/v1/student/assignments/5/submission',
    );
    expect(submission, isNotNull);
    expect(submission!.filePath, endsWith('.pdf'));
    expect(submission.grade, isNull);
  });

  test('mySubmission returns null on 404 (nothing submitted yet)', () async {
    final client = RecordingHttpClient(statusCode: 404, body: const {});
    final provider = _provider(client);

    expect(await provider.mySubmission(5), isNull);
    expect(client.requests, hasLength(1));
  });

  test('submit POSTs multipart student/assignments/{id}/submit', () async {
    final client = _MultipartStubClient();
    final provider = _provider(client);
    final bytes = List<int>.generate(16, (i) => i);

    final submission = await provider.submit(
      5,
      bytes,
      'vjezba1.pdf',
      'application/pdf',
    );

    expect(client.sent, hasLength(1));
    final request = client.sent.single;
    expect(request.method, 'POST');
    expect(request.url.path, '/api/v1/student/assignments/5/submit');
    expect(request, isA<http.MultipartRequest>());
    final multipart = request as http.MultipartRequest;
    expect(multipart.files, hasLength(1));
    final part = multipart.files.single;
    expect(part.field, 'file');
    expect(part.filename, 'vjezba1.pdf');
    expect(part.contentType.mimeType, 'application/pdf');
    expect(await part.finalize().toBytes(), bytes);
    expect(submission.assignmentId, 5);
  });

  test('myHistory issues GET student/submissions with paging', () async {
    final client = RecordingHttpClient(body: _historyBody);
    final provider = _provider(client);

    final page = await provider.myHistory(page: 2);

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'GET');
    expect(sent.url.path, '/api/v1/student/submissions');
    expect(sent.url.queryParameters['page'], '2');
    expect(sent.url.queryParameters['pageSize'], '20');
    expect(sent.url.queryParameters['includeTotalCount'], 'true');
    expect(page.items, hasLength(1));
    expect(page.items.single.assignmentId, 5);
  });
}
