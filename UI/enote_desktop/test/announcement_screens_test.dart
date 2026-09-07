import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_list_screen.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_provider.dart';
import 'package:enote_desktop/features/shared/announcement/store_announcement_provider.dart';

String _base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

String _fakeJwt({
  String subject = '1',
  String username = 'test',
  String role = 'Instructor',
}) {
  final header = _base64UrlSegment(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final payload = _base64UrlSegment(jsonEncode({
    'sub': subject,
    'unique_name': username,
    'role': role,
    'exp': (DateTime.now().millisecondsSinceEpoch ~/ 1000) + 3600,
  }));
  return '$header.$payload.signature';
}

class FakeClient extends http.BaseClient {
  final Future<http.StreamedResponse> Function(http.BaseRequest request) handler;

  FakeClient(this.handler);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) => handler(request);
}

void main() {
  testWidgets('AnnouncementListScreen renders for course provider', (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    bool fetchCalled = false;

    final client = FakeClient((request) async {
      if (request.url.toString().contains('instructor/courses/1/announcements')) {
        fetchCalled = true;
        return http.StreamedResponse(
          Stream.value(utf8.encode(jsonEncode({
            'items': [
              {
                'id': 1,
                'title': 'Test objava',
                'content': 'Sadržaj',
                'publishedAt': DateTime.now().toIso8601String(),
              }
            ],
            'page': 1,
            'pageSize': 20,
            'totalCount': 1,
          }))),
          200,
        );
      }
      return http.StreamedResponse(Stream.value(utf8.encode('{}')), 404);
    });

    final authState = AuthState(
      baseUrl: 'http://localhost/',
      httpClient: client,
      tokenReader: () => _fakeJwt(),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost/',
      authState: authState,
      httpClient: client,
    );
    final provider = AnnouncementProvider(apiClient: apiClient, courseId: 1);

    await tester.pumpWidget(MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: AnnouncementListScreen(
            provider: provider,
            title: 'Objave — Kurs 1',
          ),
        ),
      ),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Objave — Kurs 1'), findsWidgets);
    expect(find.text('Test objava'), findsOneWidget);
    expect(fetchCalled, isTrue);
  });

  testWidgets('AnnouncementListScreen renders for store provider', (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    
    bool fetchCalled = false;

    final client = FakeClient((request) async {
      if (request.url.toString().contains('shop/announcements')) {
        fetchCalled = true;
        return http.StreamedResponse(
          Stream.value(utf8.encode(jsonEncode({
            'items': [
              {
                'id': 2,
                'title': 'Store objava',
                'content': 'Sadržaj',
                'publishedAt': DateTime.now().toIso8601String(),
              }
            ],
            'page': 1,
            'pageSize': 20,
            'totalCount': 1,
          }))),
          200,
        );
      }
      return http.StreamedResponse(Stream.value(utf8.encode('{}')), 404);
    });

    final authState = AuthState(
      baseUrl: 'http://localhost/',
      httpClient: client,
      tokenReader: () => _fakeJwt(role: 'StoreEmployee'),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost/',
      authState: authState,
      httpClient: client,
    );
    final provider = StoreAnnouncementProvider(apiClient: apiClient);

    await tester.pumpWidget(MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: AnnouncementListScreen(
            provider: provider,
          ),
        ),
      ),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Objave'), findsWidgets);
    expect(find.text('Store objava'), findsOneWidget);
    expect(fetchCalled, isTrue);
  });
}
