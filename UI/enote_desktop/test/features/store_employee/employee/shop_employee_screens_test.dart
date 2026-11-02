import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_form_screen.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_list_screen.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_provider.dart';

import '../../../helpers.dart';

void main() {
  testWidgets('employee grid opens on Aktivni and sends isActive true', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = DelegatedUserHttpClient();
    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => fakeJwt(username: 'testuser', role: 'StoreEmployee'),
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

    final chip = tester.widget<ChoiceChip>(
      find.widgetWithText(ChoiceChip, 'Aktivni'),
    );
    expect(chip.selected, isTrue);

    final employeeUrls = mockClient.getUrls
        .where((url) => url.contains('shop/employees'))
        .toList();
    expect(employeeUrls, hasLength(1));
    expect(Uri.parse(employeeUrls.single).queryParameters['isActive'], 'true');
  });

  testWidgets(
    'deactivating an employee on Aktivni removes the card after refresh',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final employees = <Map<String, dynamic>>[
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

      final mockClient = ScriptedClient((request) {
        final uri = request.url;
        if (request.method == 'PUT' && uri.path.endsWith('/status')) {
          final id = int.parse(uri.pathSegments[uri.pathSegments.length - 2]);
          final body = jsonDecode(request.body) as Map<String, dynamic>;
          employees.firstWhere((e) => e['appUserId'] == id)['isActive'] =
              body['isActive'];
          return jsonResponse('', 204);
        }
        if (request.method == 'GET' && uri.path.endsWith('shop/employees')) {
          final isActive = uri.queryParameters['isActive'];
          final items = employees
              .where((e) => isActive == null || '${e['isActive']}' == isActive)
              .toList();
          return jsonResponse({
            'items': items,
            'page': 1,
            'pageSize': 10,
            'totalCount': items.length,
          }, 200);
        }
        return jsonResponse('', 404);
      });

      final authState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: mockClient,
        tokenReader: () => fakeJwt(
          username: 'testuser',
          role: 'StoreEmployee',
          isManager: true,
          subject: '20',
        ),
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

      expect(find.text('Edin Visca'), findsOneWidget);

      await tester.tap(find.byTooltip('Deaktiviraj'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Potvrdi'));
      await tester.pumpAndSettle();

      expect(mockClient.putUrls.single, endsWith('shop/employees/30/status'));
      expect(find.text('Edin Visca'), findsNothing);
      expect(find.text('Miralem Pjanic'), findsOneWidget);
    },
  );
  testWidgets(
    'ShopEmployeeListScreen hides Add button when isManager is false',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1200, 900);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = DelegatedUserHttpClient();
      final authState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: mockClient,
        tokenReader: () => fakeJwt(
          username: 'testuser',
          role: 'StoreEmployee',
          isManager: false,
        ),
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
    },
  );

  testWidgets(
    'ShopEmployeeListScreen shows Add button when isManager is true and opens form',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1200, 900);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = DelegatedUserHttpClient();
      final authState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: mockClient,
        tokenReader: () => fakeJwt(
          username: 'testuser',
          role: 'StoreEmployee',
          isManager: true,
        ),
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
      await tester.enterText(
        find.byType(TextFormField).at(1),
        'employee@example.com',
      );
      await tester.enterText(find.byType(TextFormField).at(2), 'Pass123!');
      await tester.enterText(find.byType(TextFormField).at(3), 'New');
      await tester.enterText(find.byType(TextFormField).at(4), 'Employee');

      await tester.tap(find.text('Kreiraj'));
      await tester.pumpAndSettle();

      expect(mockClient.postUrls.single, endsWith('shop/employees'));
      expect(
        mockClient.postBodies.single,
        contains('"username":"newemployee"'),
      );

      expect(find.text('Zaposlenik je kreiran.'), findsOneWidget);
      expect(find.byType(ShopEmployeeFormScreen), findsOneWidget);
    },
  );

  testWidgets(
    'ShopEmployeeListScreen: toggle renders only when isManager == true and never on caller own row',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1200, 900);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = DelegatedUserHttpClient();
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

      final nonManagerAuthState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: mockClient,
        tokenReader: () => fakeJwt(
          username: 'testuser',
          role: 'StoreEmployee',
          isManager: false,
          subject: '20',
        ),
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
              create: (_) =>
                  ShopEmployeeProvider(apiClient: nonManagerApiClient),
            ),
          ],
          child: const MaterialApp(home: ShopEmployeeListScreen()),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Miralem Pjanic'), findsOneWidget);
      expect(find.text('Edin Visca'), findsOneWidget);
      expect(find.byIcon(Icons.toggle_on), findsNothing);
      expect(find.byIcon(Icons.toggle_off), findsNothing);
      expect(find.byTooltip('Deaktiviraj'), findsNothing);
      expect(find.byTooltip('Aktiviraj'), findsNothing);

      final managerAuthState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: mockClient,
        tokenReader: () => fakeJwt(
          username: 'testuser',
          role: 'StoreEmployee',
          isManager: true,
          subject: '20',
        ),
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

      expect(find.byIcon(Icons.toggle_on), findsOneWidget);
      expect(find.byTooltip('Deaktiviraj'), findsOneWidget);

      await tester.tap(find.byTooltip('Deaktiviraj'));
      await tester.pumpAndSettle();

      expect(find.text('Potvrdite deaktivaciju'), findsOneWidget);
      expect(
        find.text(
          'Da li ste sigurni da želite da deaktivirate ovog korisnika?',
        ),
        findsOneWidget,
      );

      await tester.tap(find.text('Potvrdi'));
      await tester.pumpAndSettle();

      expect(mockClient.putUrls.single, endsWith('shop/employees/30/status'));
      expect(mockClient.putBodies.single, contains('"isActive":false'));
    },
  );
}
