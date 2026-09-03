import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_form_screen.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_list_screen.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_form_screen.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_list_screen.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_provider.dart';

String _base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

String _fakeJwt({
  String subject = '1',
  String username = 'testuser',
  String role = 'Instructor',
  bool isManager = false,
}) {
  final header = _base64UrlSegment(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final payload = _base64UrlSegment(jsonEncode({
    'sub': subject,
    'unique_name': username,
    'role': role,
    if (isManager) 'is_manager': true,
    'exp': DateTime.now()
            .add(const Duration(days: 1))
            .millisecondsSinceEpoch ~/
        1000,
  }));
  return '$header.$payload.signature';
}

class _MockHttpClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final url = request.url.toString();

    if (request.method == 'GET' && url.contains('instructor/students')) {
      final json = jsonEncode({
        'items': [
          {
            'id': 1,
            'appUserId': 10,
            'firstName': 'Edin',
            'lastName': 'Dzeko',
            'username': 'edzeko',
          }
        ],
        'page': 1,
        'pageSize': 10,
        'totalCount': 1,
      });
      return http.StreamedResponse(
        Stream.value(utf8.encode(json)),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    if (request.method == 'GET' && url.contains('shop/employees')) {
      final json = jsonEncode({
        'items': [
          {
            'id': 1,
            'appUserId': 20,
            'musicStoreId': 1,
            'firstName': 'Miralem',
            'lastName': 'Pjanic',
            'username': 'mpjanic',
            'isManager': true,
            'isActive': true,
          }
        ],
        'page': 1,
        'pageSize': 10,
        'totalCount': 1,
      });
      return http.StreamedResponse(
        Stream.value(utf8.encode(json)),
        200,
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
  testWidgets('InstructorStudentListScreen displays student and opens form',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = _MockHttpClient();
    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => _fakeJwt(role: 'Instructor'),
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
          ChangeNotifierProvider<InstructorStudentProvider>(
            create: (_) => InstructorStudentProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(home: InstructorStudentListScreen()),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Studenti'), findsWidgets);
    expect(find.text('Kreiraj studenta'), findsOneWidget);

    await tester.tap(find.text('Kreiraj studenta'));
    await tester.pumpAndSettle();

    expect(find.byType(InstructorStudentFormScreen), findsOneWidget);
  });

  testWidgets('ShopEmployeeListScreen hides Add button when isManager is false',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = _MockHttpClient();
    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => _fakeJwt(role: 'StoreEmployee', isManager: false),
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
          ChangeNotifierProvider<ShopEmployeeProvider>(
            create: (_) => ShopEmployeeProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(home: ShopEmployeeListScreen()),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Zaposlenici'), findsWidgets);
    expect(find.text('Kreiraj zaposlenika'), findsNothing);
  });

  testWidgets(
      'ShopEmployeeListScreen shows Add button when isManager is true and opens form',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = _MockHttpClient();
    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => _fakeJwt(role: 'StoreEmployee', isManager: true),
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
          ChangeNotifierProvider<ShopEmployeeProvider>(
            create: (_) => ShopEmployeeProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(home: ShopEmployeeListScreen()),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Zaposlenici'), findsWidgets);
    expect(find.text('Kreiraj zaposlenika'), findsOneWidget);

    await tester.tap(find.text('Kreiraj zaposlenika'));
    await tester.pumpAndSettle();

    expect(find.byType(ShopEmployeeFormScreen), findsOneWidget);
  });
}
