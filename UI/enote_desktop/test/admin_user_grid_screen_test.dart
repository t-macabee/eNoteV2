import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/users/admin_user_provider.dart';
import 'package:enote_desktop/features/admin/users/user_grid_screen.dart';

import 'helpers.dart';

const _users = [
  {
    'appUserId': 10,
    'username': 'aadmin',
    'firstName': 'Ada',
    'lastName': 'Admin',
    'role': 'Administrator',
    'isActive': true,
  },
  {
    'appUserId': 11,
    'username': 'wmozart',
    'firstName': 'Wolfgang',
    'lastName': 'Mozart',
    'role': 'Instructor',
    'isActive': true,
  },
  {
    'appUserId': 22,
    'username': 'lbeethoven',
    'firstName': 'Ludwig',
    'lastName': 'Beethoven',
    'role': 'Student',
    'isActive': true,
    'studentId': 2,
    'enrollmentDate': '2026-01-10T00:00:00Z',
    'membershipPaidUntil': '2026-12-31T00:00:00Z',
  },
  {
    'appUserId': 33,
    'username': 'jbach',
    'firstName': 'Johann',
    'lastName': 'Bach',
    'role': 'StoreEmployee',
    'isActive': true,
    'storeName': 'Sarajevo Guitar House',
    'isManager': true,
  },
];

ScriptedClient _client({int totalCount = 130, bool failProfile = false}) => ScriptedClient((request) {
  final uri = request.url;

  if (request.method == 'GET' && RegExp(r'/admin/users/\d+$').hasMatch(uri.path)) {
    if (failProfile) {
      return jsonResponse({'message': 'boom'}, 500);
    }
    final id = int.parse(uri.pathSegments.last);
    return jsonResponse({
      'role': 'Instructor',
      'username': 'wmozart',
      'email': 'w@example.com',
      'profile': {
        'id': id,
        'firstName': 'Wolfgang',
        'lastName': 'Mozart',
        'isActive': true,
      },
    }, 200);
  }

  if (request.method == 'GET' && uri.path.endsWith('/admin/users')) {
    final role = uri.queryParameters['role'];
    final name = uri.queryParameters['name']?.toLowerCase();
    final isActive = uri.queryParameters['isActive'];

    var filtered = _users;

    if (role != null && role.isNotEmpty) {
      filtered = filtered.where((u) => u['role'] == role).toList();
    }

    if (isActive != null) {
      final active = isActive == 'true';
      filtered = filtered.where((u) => u['isActive'] == active).toList();
    }

    if (name != null && name.isNotEmpty) {
      filtered = filtered.where((u) {
        final haystack =
            '${u['firstName']} ${u['lastName']} ${u['username']}'.toLowerCase();
        return haystack.contains(name);
      }).toList();
    }

    final hasFilters =
        (role != null && role.isNotEmpty) || (name != null && name.isNotEmpty);

    return jsonResponse({
      'items': filtered,
      'page': int.tryParse(uri.queryParameters['page'] ?? '1') ?? 1,
      'pageSize': int.tryParse(uri.queryParameters['pageSize'] ?? '24') ?? 24,
      'totalCount': hasFilters ? filtered.length : totalCount,
    }, 200);
  }

  if (request.method == 'DELETE' || request.method == 'PUT') {
    return jsonResponse('', 204);
  }

  return jsonResponse(const {}, 200);
});

Future<void> _pumpGrid(WidgetTester tester, ScriptedClient mockClient) async {
  tester.view.physicalSize = const Size(1400, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    httpClient: mockClient,
    tokenReader: () => fakeJwt(),
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
        ChangeNotifierProvider<AdminUserProvider>(
          create: (_) => AdminUserProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(home: UserGridScreen()),
    ),
  );

  await tester.pumpAndSettle();
}

List<Uri> _adminUserRequests(ScriptedClient client) => [
      for (final request in client.requests)
        if (request.method == 'GET' && request.url.path.endsWith('/admin/users'))
          request.url,
    ];

void main() {
  testWidgets(
    'UserGridScreen loads one paged request for all roles and uses the server totalCount',
    (WidgetTester tester) async {
      final mockClient = _client();
      await _pumpGrid(tester, mockClient);

      final requests = _adminUserRequests(mockClient);
      expect(requests, hasLength(1));
      expect(requests.single.queryParameters['page'], '1');
      expect(requests.single.queryParameters['isActive'], 'true');
      expect(
        mockClient.requestedUrls.any((u) => u.contains('admin/instructors')),
        isFalse,
      );
      expect(
        mockClient.requestedUrls.any((u) => u.contains('admin/students')),
        isFalse,
      );
      expect(
        mockClient.requestedUrls.any((u) => u.contains('admin/employees')),
        isFalse,
      );

      expect(find.text('Ada Admin'), findsOneWidget);
      expect(find.text('Wolfgang Mozart'), findsOneWidget);
      expect(find.text('Ludwig Beethoven'), findsOneWidget);
      expect(find.text('Johann Bach'), findsOneWidget);
      expect(find.text('Sarajevo Guitar House'), findsOneWidget);

      expect(find.text('Ukupno: 130'), findsOneWidget);
      expect(find.text('Stranica 1 od 6'), findsOneWidget);

      await tester.tap(find.text('Sljedeća'));
      await tester.pumpAndSettle();

      final nextRequests = _adminUserRequests(mockClient);
      expect(nextRequests, hasLength(2));
      expect(nextRequests.last.queryParameters['page'], '2');
      expect(find.text('Stranica 2 od 6'), findsOneWidget);
    },
  );

  testWidgets(
    'UserGridScreen filters by Administrator role with the server role filter',
    (WidgetTester tester) async {
      final mockClient = _client();
      await _pumpGrid(tester, mockClient);

      mockClient.requests.clear();

      await tester.tap(find.byType(DropdownButtonFormField<UserRole?>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Administrator').last);
      await tester.pumpAndSettle();

      final requests = _adminUserRequests(mockClient);
      expect(requests, hasLength(1));
      expect(requests.single.queryParameters['role'], 'Administrator');

      expect(find.text('Ada Admin'), findsOneWidget);
      expect(find.text('Wolfgang Mozart'), findsNothing);
      expect(find.text('Ludwig Beethoven'), findsNothing);
      expect(find.text('Johann Bach'), findsNothing);
    },
  );

  testWidgets(
    'UserGridScreen searches by name and can deactivate a user',
    (WidgetTester tester) async {
      final mockClient = _client();
      await _pumpGrid(tester, mockClient);

      final searchField =
          find.widgetWithText(TextField, 'Pretraži po imenu...');
      await tester.enterText(searchField, 'Bach');
      await pumpPastDebounce(tester);
      await tester.pumpAndSettle();

      expect(find.text('Johann Bach'), findsOneWidget);
      expect(find.text('Wolfgang Mozart'), findsNothing);
      expect(find.text('Sarajevo Guitar House'), findsOneWidget);

      await tester.tap(find.text('Johann Bach'));
      await tester.pumpAndSettle();

      final deactivateButton = find.text('Deaktiviraj');
      expect(deactivateButton, findsOneWidget);

      await tester.tap(deactivateButton);
      await tester.pumpAndSettle();

      expect(find.text('Potvrdite deaktivaciju'), findsOneWidget);
      expect(
        find.text(
          'Da li ste sigurni da želite da deaktivirate korisnika "Johann Bach"?',
        ),
        findsOneWidget,
      );

      await tester.tap(find.text('Potvrdi'));
      await tester.pumpAndSettle();

      expect(mockClient.putUrls, hasLength(1));
      expect(mockClient.putUrls[0], endsWith('admin/users/33/status'));
    },
  );

  testWidgets(
    'UserGridScreen refreshes after the user details dialog closes',
    (WidgetTester tester) async {
      final mockClient = _client();
      await _pumpGrid(tester, mockClient);

      await tester.tap(find.text('Wolfgang Mozart'));
      await tester.pumpAndSettle();

      mockClient.requests.clear();

      await tester.tap(find.text('Zatvori'));
      await tester.pumpAndSettle();

      expect(_adminUserRequests(mockClient), hasLength(1));
    },
  );

  testWidgets(
    'UserGridScreen deletes a user permanently',
    (WidgetTester tester) async {
      final mockClient = _client();
      await _pumpGrid(tester, mockClient);

      final searchField =
          find.widgetWithText(TextField, 'Pretraži po imenu...');
      await tester.enterText(searchField, 'Bach');
      await pumpPastDebounce(tester);
      await tester.pumpAndSettle();

      await tester.tap(find.text('Johann Bach'));
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.delete_outline));
      await tester.pumpAndSettle();

      expect(find.text('Trajno brisanje'), findsOneWidget);

      await tester.tap(find.text('Potvrdi'));
      await tester.pumpAndSettle();

      expect(mockClient.deletedUrls, hasLength(1));
      expect(mockClient.deletedUrls[0], endsWith('admin/users/33'));
    },
  );

  testWidgets(
    'UserDetailsDialog shows an error text when the profile fetch fails',
    (WidgetTester tester) async {
      final mockClient = _client(failProfile: true);
      await _pumpGrid(tester, mockClient);

      await tester.tap(find.text('Wolfgang Mozart'));
      await tester.pumpAndSettle();

      expect(find.text('Greška pri učitavanju profila.'), findsOneWidget);
    },
  );
}
