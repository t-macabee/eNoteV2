import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/event/event_form_screen.dart';
import 'package:enote_desktop/features/admin/event/event_list_screen.dart';
import 'package:enote_desktop/features/admin/event/event_provider.dart';
import 'package:enote_desktop/features/admin/instructor/instructor_provider.dart';

String _base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

String _fakeJwt({
  String subject = '1',
  String username = 'admin',
  String role = 'Administrator',
}) {
  final header = _base64UrlSegment(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final payload = _base64UrlSegment(jsonEncode({
    'sub': subject,
    'unique_name': username,
    'role': role,
    'exp': DateTime.now()
            .add(const Duration(days: 1))
            .millisecondsSinceEpoch ~/
        1000,
  }));
  return '$header.$payload.signature';
}

class _AdminEventMockHttpClient extends http.BaseClient {
  final List<String> requestedUrls = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final url = request.url.toString();
    requestedUrls.add(url);

    if (request.method == 'GET') {
      if (url.contains('admin/instructors')) {
        final json = jsonEncode({
          'items': [],
          'page': 1,
          'pageSize': 200,
          'totalCount': 0,
        });
        return http.StreamedResponse(
          Stream.value(utf8.encode(json)),
          200,
          headers: {'content-type': 'application/json'},
        );
      }

      if (url.contains('admin/addresses')) {
        final json = jsonEncode({
          'items': [],
          'page': 1,
          'pageSize': 200,
          'totalCount': 0,
        });
        return http.StreamedResponse(
          Stream.value(utf8.encode(json)),
          200,
          headers: {'content-type': 'application/json'},
        );
      }

      if (url.contains('admin/events')) {
        final json = jsonEncode({
          'items': [
            {
              'id': 1,
              'title': 'Platform Gala',
              'description': 'Gala concert across all departments',
              'startsAt': '2026-09-10T18:00:00Z',
              'endsAt': '2026-09-10T20:00:00Z',
              'addressId': null,
              'courseId': null,
              'instructorId': null,
            },
            {
              'id': 2,
              'title': 'Guitar Masterclass',
              'description': 'Masterclass scoped to course and instructor',
              'startsAt': '2026-09-12T14:00:00Z',
              'endsAt': '2026-09-12T16:00:00Z',
              'addressId': null,
              'courseId': 10,
              'courseName': 'Classical Guitar I',
              'instructorId': 5,
            },
          ],
          'page': 1,
          'pageSize': 24,
          'totalCount': 2,
        });
        return http.StreamedResponse(
          Stream.value(utf8.encode(json)),
          200,
          headers: {'content-type': 'application/json'},
        );
      }
    }

    return http.StreamedResponse(
      Stream.value(utf8.encode('{}')),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

Widget _buildTestApp(_AdminEventMockHttpClient mockClient) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    httpClient: mockClient,
    tokenReader: () => _fakeJwt(),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: mockClient,
  );

  return MultiProvider(
    providers: [
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<AuthState>.value(value: authState),
      ChangeNotifierProvider<InstructorProvider>(
        create: (_) => InstructorProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<EventProvider>(
        create: (_) => EventProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<AddressProvider>(
        create: (_) => AddressProvider(apiClient: apiClient),
      ),
    ],
    child: const MaterialApp(home: EventListScreen()),
  );
}

void main() {
  testWidgets(
    'EventListScreen displays both platform-wide and scoped events with read-only badge on scoped events',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _AdminEventMockHttpClient();
      await tester.pumpWidget(_buildTestApp(mockClient));
      await tester.pumpAndSettle();

      // Both events rendered
      expect(find.text('Platform Gala'), findsOneWidget);
      expect(find.text('Guitar Masterclass'), findsOneWidget);

      // Scoped event has visual badge "Samo pregled"
      expect(find.text('Samo pregled'), findsOneWidget);
    },
  );

  testWidgets(
    'Tapping a scoped event displays ErrorBanner without opening EventFormScreen',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _AdminEventMockHttpClient();
      await tester.pumpWidget(_buildTestApp(mockClient));
      await tester.pumpAndSettle();

      // Tap on scoped event
      await tester.tap(find.text('Guitar Masterclass'));
      await tester.pumpAndSettle();

      // Error banner is displayed explaining why admin cannot edit scoped event
      expect(
        find.text(
          'Administrator može upravljati samo događajima na nivou platforme.',
        ),
        findsOneWidget,
      );

      // EventFormScreen is NOT opened
      expect(find.byType(EventFormScreen), findsNothing);
    },
  );

  testWidgets(
    'Tapping a platform-wide event opens EventFormScreen dialog for editing',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _AdminEventMockHttpClient();
      await tester.pumpWidget(_buildTestApp(mockClient));
      await tester.pumpAndSettle();

      // Tap on platform-wide event
      await tester.tap(find.text('Platform Gala'));
      await tester.pumpAndSettle();

      // EventFormScreen is opened in edit mode
      expect(find.byType(EventFormScreen), findsOneWidget);
      expect(find.text('Uredi događaj'), findsOneWidget);
    },
  );
}
