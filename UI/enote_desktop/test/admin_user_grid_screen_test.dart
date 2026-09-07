import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/instructor/instructor_provider.dart';
import 'package:enote_desktop/features/admin/student/student_provider.dart';
import 'package:enote_desktop/features/admin/users/admin_user_provider.dart';
import 'package:enote_desktop/features/admin/users/store_employee_provider.dart';
import 'package:enote_desktop/features/admin/users/user_grid_screen.dart';

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

class _AdminUsersMockHttpClient extends http.BaseClient {
  final List<String> requestedUrls = [];
  final List<String> deletedUrls = [];
  final List<String> putUrls = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final url = request.url.toString();

    if (request.method == 'GET') {
      requestedUrls.add(url);

      if (url.contains('admin/instructors')) {
        final json = jsonEncode({
          'items': [
            {
              'id': 1,
              'appUserId': 11,
              'firstName': 'Wolfgang',
              'lastName': 'Mozart',
              'username': 'wmozart',
            }
          ],
          'page': 1,
          'pageSize': 20,
          'totalCount': 1,
        });
        return http.StreamedResponse(
          Stream.value(utf8.encode(json)),
          200,
          headers: {'content-type': 'application/json'},
        );
      }

      if (url.contains('admin/students')) {
        final json = jsonEncode({
          'items': [
            {
              'id': 2,
              'appUserId': 22,
              'firstName': 'Ludwig',
              'lastName': 'Beethoven',
              'username': 'lbeethoven',
            }
          ],
          'page': 1,
          'pageSize': 20,
          'totalCount': 1,
        });
        return http.StreamedResponse(
          Stream.value(utf8.encode(json)),
          200,
          headers: {'content-type': 'application/json'},
        );
      }

      if (url.contains('admin/employees')) {
        final queryName = request.url.queryParameters['name'];
        final allEmployees = [
          {
            'id': 3,
            'appUserId': 33,
            'musicStoreId': 5,
            'storeName': 'Sarajevo Guitar House',
            'firstName': 'Johann',
            'lastName': 'Bach',
            'username': 'jbach',
            'isManager': true,
            'isActive': true,
          },
          {
            'id': 4,
            'appUserId': 44,
            'musicStoreId': 6,
            'storeName': 'Mostar Piano Center',
            'firstName': 'Frederic',
            'lastName': 'Chopin',
            'username': 'fchopin',
            'isManager': false,
            'isActive': true,
          },
        ];

        final filtered = queryName == null || queryName.isEmpty
            ? allEmployees
            : allEmployees.where((e) {
                final fn = (e['firstName'] as String).toLowerCase();
                final ln = (e['lastName'] as String).toLowerCase();
                final un = (e['username'] as String).toLowerCase();
                final q = queryName.toLowerCase();
                return fn.contains(q) || ln.contains(q) || un.contains(q);
              }).toList();

        final json = jsonEncode({
          'items': filtered,
          'page': 1,
          'pageSize': 20,
          'totalCount': filtered.length,
        });
        return http.StreamedResponse(
          Stream.value(utf8.encode(json)),
          200,
          headers: {'content-type': 'application/json'},
        );
      }

      if (url.contains('admin/users')) {
        final json = jsonEncode({
          'role': 'StoreEmployee',
          'profile': {
            'id': 4,
            'firstName': 'Frederic',
            'lastName': 'Chopin',
            'username': 'fchopin',
            'isActive': true,
          },
        });
        return http.StreamedResponse(
          Stream.value(utf8.encode(json)),
          200,
          headers: {'content-type': 'application/json'},
        );
      }
    }

    if (request.method == 'DELETE') {
      deletedUrls.add(url);
      return http.StreamedResponse(
        Stream.value(utf8.encode('')),
        204,
        headers: {'content-type': 'application/json'},
      );
    }

    if (request.method == 'PUT') {
      putUrls.add(url);
      return http.StreamedResponse(
        Stream.value(utf8.encode('')),
        204,
        headers: {'content-type': 'application/json'},
      );
    }

    return http.StreamedResponse(
      Stream.value(utf8.encode('{}')),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets(
    'UserGridScreen displays all roles including StoreEmployees with their store names',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _AdminUsersMockHttpClient();
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

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            Provider<ApiClient>.value(value: apiClient),
            ChangeNotifierProvider<AuthState>.value(value: authState),
            ChangeNotifierProvider<InstructorProvider>(
              create: (_) => InstructorProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<StudentProvider>(
              create: (_) => StudentProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<StoreEmployeeProvider>(
              create: (_) => StoreEmployeeProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<AdminUserProvider>(
              create: (_) => AdminUserProvider(apiClient: apiClient),
            ),
          ],
          child: const MaterialApp(home: UserGridScreen()),
        ),
      );

      await tester.pumpAndSettle();

      // "Svi korisnici" by default queries all three endpoints
      expect(mockClient.requestedUrls.any((u) => u.contains('admin/instructors')), isTrue);
      expect(mockClient.requestedUrls.any((u) => u.contains('admin/students')), isTrue);
      expect(mockClient.requestedUrls.any((u) => u.contains('admin/employees')), isTrue);

      // Verify names rendered across roles
      expect(find.text('Wolfgang Mozart'), findsOneWidget);
      expect(find.text('Ludwig Beethoven'), findsOneWidget);
      expect(find.text('Johann Bach'), findsOneWidget);
      expect(find.text('Frederic Chopin'), findsOneWidget);

      // Verify store name labels rendered for employees
      expect(find.text('Sarajevo Guitar House'), findsOneWidget);
      expect(find.text('Mostar Piano Center'), findsOneWidget);

      // Section labels
      expect(find.text('Instruktori'), findsOneWidget);
      expect(find.text('Studenti'), findsOneWidget);
      expect(find.text('StoreEmployee'), findsOneWidget);
    },
  );

  testWidgets(
    'UserGridScreen filters to StoreEmployee role via dropdown',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _AdminUsersMockHttpClient();
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

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            Provider<ApiClient>.value(value: apiClient),
            ChangeNotifierProvider<AuthState>.value(value: authState),
            ChangeNotifierProvider<InstructorProvider>(
              create: (_) => InstructorProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<StudentProvider>(
              create: (_) => StudentProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<StoreEmployeeProvider>(
              create: (_) => StoreEmployeeProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<AdminUserProvider>(
              create: (_) => AdminUserProvider(apiClient: apiClient),
            ),
          ],
          child: const MaterialApp(home: UserGridScreen()),
        ),
      );

      await tester.pumpAndSettle();

      mockClient.requestedUrls.clear();

      // Open role dropdown
      await tester.tap(find.byType(DropdownButtonFormField<UserRole?>));
      await tester.pumpAndSettle();

      // Pick "StoreEmployee"
      await tester.tap(find.text('StoreEmployee').last);
      await tester.pumpAndSettle();

      // Should query only admin/employees
      expect(mockClient.requestedUrls.any((u) => u.contains('admin/employees')), isTrue);
      expect(mockClient.requestedUrls.any((u) => u.contains('admin/instructors')), isFalse);
      expect(mockClient.requestedUrls.any((u) => u.contains('admin/students')), isFalse);

      // Employees are shown, instructors and students are not
      expect(find.text('Johann Bach'), findsOneWidget);
      expect(find.text('Frederic Chopin'), findsOneWidget);
      expect(find.text('Wolfgang Mozart'), findsNothing);
      expect(find.text('Ludwig Beethoven'), findsNothing);

      // Store names are visible
      expect(find.text('Sarajevo Guitar House'), findsOneWidget);
      expect(find.text('Mostar Piano Center'), findsOneWidget);
    },
  );

  testWidgets(
    'UserGridScreen searches StoreEmployees by name and can deactivate one',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _AdminUsersMockHttpClient();
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

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            Provider<ApiClient>.value(value: apiClient),
            ChangeNotifierProvider<AuthState>.value(value: authState),
            ChangeNotifierProvider<InstructorProvider>(
              create: (_) => InstructorProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<StudentProvider>(
              create: (_) => StudentProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<StoreEmployeeProvider>(
              create: (_) => StoreEmployeeProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<AdminUserProvider>(
              create: (_) => AdminUserProvider(apiClient: apiClient),
            ),
          ],
          child: const MaterialApp(home: UserGridScreen()),
        ),
      );

      await tester.pumpAndSettle();

      // Filter to StoreEmployee
      await tester.tap(find.byType(DropdownButtonFormField<UserRole?>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('StoreEmployee').last);
      await tester.pumpAndSettle();

      // Search for "Chopin"
      final searchField = find.widgetWithText(TextField, 'Pretraži po imenu...');
      await tester.enterText(searchField, 'Chopin');
      await tester.pump(const Duration(milliseconds: 400));
      await tester.pumpAndSettle();

      expect(find.text('Frederic Chopin'), findsOneWidget);
      expect(find.text('Mostar Piano Center'), findsOneWidget);
      expect(find.text('Johann Bach'), findsNothing);

      // Open details dialog for Frederic Chopin
      await tester.tap(find.text('Frederic Chopin'));
      await tester.pumpAndSettle();

      // Deaktiviraj button should be visible in dialog
      final deactivateButton = find.text('Deaktiviraj');
      expect(deactivateButton, findsOneWidget);

      // Tap Deaktiviraj to open confirm dialog
      await tester.tap(deactivateButton);
      await tester.pumpAndSettle();

      // Assert confirm dialog wording
      expect(find.text('Potvrdite deaktivaciju'), findsOneWidget);
      expect(
        find.text('Da li ste sigurni da želite da deaktivirate ovog korisnika?'),
        findsOneWidget,
      );

      // Confirm deactivation
      await tester.tap(find.text('Potvrdi'));
      await tester.pumpAndSettle();

      // Assert PUT admin/users/44/status was called
      expect(mockClient.putUrls, hasLength(1));
      expect(mockClient.putUrls[0], endsWith('admin/users/44/status'));
    },
  );

  testWidgets(
    'UserGridScreen deletes a user permanently',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _AdminUsersMockHttpClient();
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

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            Provider<ApiClient>.value(value: apiClient),
            ChangeNotifierProvider<AuthState>.value(value: authState),
            ChangeNotifierProvider<InstructorProvider>(
              create: (_) => InstructorProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<StudentProvider>(
              create: (_) => StudentProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<StoreEmployeeProvider>(
              create: (_) => StoreEmployeeProvider(apiClient: apiClient),
            ),
            ChangeNotifierProvider<AdminUserProvider>(
              create: (_) => AdminUserProvider(apiClient: apiClient),
            ),
          ],
          child: const MaterialApp(home: UserGridScreen()),
        ),
      );

      await tester.pumpAndSettle();

      // Filter to StoreEmployee
      await tester.tap(find.byType(DropdownButtonFormField<UserRole?>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('StoreEmployee').last);
      await tester.pumpAndSettle();

      // Search for "Chopin"
      final searchField = find.widgetWithText(TextField, 'Pretraži po imenu...');
      await tester.enterText(searchField, 'Chopin');
      await tester.pump(const Duration(milliseconds: 400));
      await tester.pumpAndSettle();

      // Open details dialog for Frederic Chopin
      await tester.tap(find.text('Frederic Chopin'));
      await tester.pumpAndSettle();

      // Tap trash icon
      await tester.tap(find.byIcon(Icons.delete_outline));
      await tester.pumpAndSettle();

      // Assert confirm dialog wording
      expect(find.text('Trajno brisanje'), findsOneWidget);

      // Confirm deletion
      await tester.tap(find.text('Potvrdi'));
      await tester.pumpAndSettle();

      // Assert DELETE admin/users/44 was called
      expect(mockClient.deletedUrls, hasLength(1));
      expect(mockClient.deletedUrls[0], endsWith('admin/users/44'));
    },
  );
}
