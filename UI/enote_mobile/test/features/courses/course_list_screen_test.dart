import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/courses/course_list_screen.dart';
import 'package:enote_mobile/features/courses/course_provider.dart';

import '../../helpers.dart';

class _CourseListStubClient extends http.BaseClient {
  final List<http.Request> requests = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request is http.Request) {
      requests.add(request);
    }
    const body = {'items': [], 'page': 1, 'pageSize': 20, 'totalCount': 0};
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('F6-06: course list query matches pagedQuery(1, 20) output',
      (tester) async {
    final client = _CourseListStubClient();
    final authState = AuthState(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      httpClient: client,
    );
    final apiClient = ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthState>.value(value: authState),
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<CourseProvider>(
            create: (_) => CourseProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(
          home: Scaffold(body: CourseListScreen()),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(client.requests, isNotEmpty);
    final expected = pagedQuery(1, 20, '')
        .map((k, v) => MapEntry(k, v.toString()));
    expect(client.requests.first.url.queryParameters, equals(expected));
  });
}
