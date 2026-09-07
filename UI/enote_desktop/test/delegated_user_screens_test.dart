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
  final List<String> postUrls = [];
  final List<String> postBodies = [];
  final List<String> putUrls = [];
  final List<String> putBodies = [];
  final List<String> getUrls = [];
  bool simulate400 = false;
  List<Map<String, dynamic>>? shopEmployees;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final url = request.url.toString();
    if (request.method == 'GET') {
      getUrls.add(url);
    }

    if (request.method == 'POST') {
      if (request is http.Request) {
        postUrls.add(url);
        postBodies.add(request.body);
      }
      if (simulate400) {
        return http.StreamedResponse(
          Stream.value(utf8.encode(jsonEncode({'message': 'Neispravan zahtjev.'}))),
          400,
          headers: {'content-type': 'application/json'},
        );
      }
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode({'userId': 42}))),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    if (request.method == 'PUT') {
      if (request is http.Request) {
        putUrls.add(url);
        putBodies.add(request.body);
      }
      return http.StreamedResponse(
        Stream.value(utf8.encode('')),
        204,
        headers: {'content-type': 'application/json'},
      );
    }

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
      final items = shopEmployees ?? [
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
      ];
      final json = jsonEncode({
        'items': items,
        'page': 1,
        'pageSize': 10,
        'totalCount': items.length,
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

    expect(find.text('Edin Dzeko'), findsOneWidget);
    expect(find.text('Kreiraj studenta'), findsOneWidget);

    await tester.tap(find.text('Kreiraj studenta'));
    await tester.pumpAndSettle();

    expect(find.byType(InstructorStudentFormScreen), findsOneWidget);

    await tester.enterText(find.byType(TextFormField).at(0), 'newstudent');
    await tester.enterText(find.byType(TextFormField).at(1), 'student@example.com');
    await tester.enterText(find.byType(TextFormField).at(2), 'Pass123!');
    await tester.enterText(find.byType(TextFormField).at(3), 'New');
    await tester.enterText(find.byType(TextFormField).at(4), 'Student');
    
    await tester.tap(find.text('Kreiraj'));
    await tester.pumpAndSettle();

    expect(mockClient.postUrls.single, endsWith('instructor/students'));
    expect(mockClient.postBodies.single, contains('"username":"newstudent"'));
    
    // EntityFormScaffold does not pop in create mode, it resets and shows a snackbar.
    expect(find.text('Uspješno sačuvano.'), findsOneWidget);
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

    expect(find.text('Miralem Pjanic'), findsOneWidget);
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

    expect(find.text('Miralem Pjanic'), findsOneWidget);
    expect(find.text('Kreiraj zaposlenika'), findsOneWidget);

    await tester.tap(find.text('Kreiraj zaposlenika'));
    await tester.pumpAndSettle();

    expect(find.byType(ShopEmployeeFormScreen), findsOneWidget);

    await tester.enterText(find.byType(TextFormField).at(0), 'newemployee');
    await tester.enterText(find.byType(TextFormField).at(1), 'employee@example.com');
    await tester.enterText(find.byType(TextFormField).at(2), 'Pass123!');
    await tester.enterText(find.byType(TextFormField).at(3), 'New');
    await tester.enterText(find.byType(TextFormField).at(4), 'Employee');
    
    await tester.tap(find.text('Kreiraj'));
    await tester.pumpAndSettle();

    expect(mockClient.postUrls.single, endsWith('shop/employees'));
    expect(mockClient.postBodies.single, contains('"username":"newemployee"'));
    
    // EntityFormScaffold does not pop in create mode, it resets and shows a snackbar.
    expect(find.text('Uspješno sačuvano.'), findsOneWidget);
    expect(find.byType(ShopEmployeeFormScreen), findsOneWidget);
  });

  testWidgets('400 response produces mapped Bosnian message',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = _MockHttpClient();
    mockClient.simulate400 = true;
    
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
        child: const MaterialApp(home: InstructorStudentFormScreen()),
      ),
    );

    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextFormField).at(0), 'badstudent');
    await tester.enterText(find.byType(TextFormField).at(1), 'bad@example.com');
    await tester.enterText(find.byType(TextFormField).at(2), 'Pass123!');
    
    await tester.tap(find.text('Kreiraj'));
    await tester.pumpAndSettle();

    expect(find.text('Neispravan zahtjev.'), findsOneWidget);
  });

  testWidgets('Fetcher query map includes includeTotalCount for pagination',
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

    expect(mockClient.getUrls.any((url) => url.contains('includeTotalCount=true')), isTrue);
  });

  testWidgets(
      'ShopEmployeeListScreen: toggle renders only when isManager == true and never on caller own row',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = _MockHttpClient();
    mockClient.shopEmployees = [
      {
        'id': 1,
        'appUserId': 20,
        'musicStoreId': 1,
        'firstName': 'Miralem',
        'lastName': 'Pjanic',
        'username': 'mpjanic',
        'isManager': true,
        'isActive': true,
      },
      {
        'id': 2,
        'appUserId': 30,
        'musicStoreId': 1,
        'firstName': 'Edin',
        'lastName': 'Visca',
        'username': 'evisca',
        'isManager': false,
        'isActive': true,
      },
    ];

    // Case 1: Caller is NOT a manager (isManager: false, subject: '20')
    final nonManagerAuthState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => _fakeJwt(role: 'StoreEmployee', isManager: false, subject: '20'),
    );
    final nonManagerApiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: nonManagerAuthState,
      httpClient: mockClient,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: nonManagerApiClient),
          ChangeNotifierProvider<AuthState>.value(value: nonManagerAuthState),
          ChangeNotifierProvider<ShopEmployeeProvider>(
            create: (_) => ShopEmployeeProvider(apiClient: nonManagerApiClient),
          ),
        ],
        child: const MaterialApp(home: ShopEmployeeListScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Miralem Pjanic'), findsOneWidget);
    expect(find.text('Edin Visca'), findsOneWidget);
    // Non-manager: No toggle buttons rendered
    expect(find.byIcon(Icons.toggle_on), findsNothing);
    expect(find.byIcon(Icons.toggle_off), findsNothing);
    expect(find.byTooltip('Deaktiviraj'), findsNothing);
    expect(find.byTooltip('Aktiviraj'), findsNothing);

    // Case 2: Caller IS a manager (isManager: true, subject: '20')
    final managerAuthState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => _fakeJwt(role: 'StoreEmployee', isManager: true, subject: '20'),
    );
    final managerApiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: managerAuthState,
      httpClient: mockClient,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: managerApiClient),
          ChangeNotifierProvider<AuthState>.value(value: managerAuthState),
          ChangeNotifierProvider<ShopEmployeeProvider>(
            create: (_) => ShopEmployeeProvider(apiClient: managerApiClient),
          ),
        ],
        child: const MaterialApp(home: ShopEmployeeListScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Miralem Pjanic'), findsOneWidget);
    expect(find.text('Edin Visca'), findsOneWidget);

    // Toggle renders on Edin Visca's card (appUserId 30 != 20), but NEVER on caller's card (appUserId 20 == 20)
    expect(find.byIcon(Icons.toggle_on), findsOneWidget);
    expect(find.byTooltip('Deaktiviraj'), findsOneWidget);

    // Tapping toggle on Edin Visca triggers confirmation dialog
    await tester.tap(find.byTooltip('Deaktiviraj'));
    await tester.pumpAndSettle();

    expect(find.text('Potvrdite deaktivaciju'), findsOneWidget);
    expect(
        find.text('Da li ste sigurni da želite da deaktivirate ovog korisnika?'),
        findsOneWidget);

    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(mockClient.putUrls.single, endsWith('shop/employees/30/status'));
    expect(mockClient.putBodies.single, contains('"isActive":false'));
  });
}
