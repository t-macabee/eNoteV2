import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';
import 'package:enote_desktop/features/admin/users/admin_user_provider.dart';
import 'package:enote_desktop/features/admin/users/user_provision_form_screen.dart';

import '../../../helpers.dart';

const _message =
    'Nema prodavnica. Prvo kreirajte prodavnicu u dijelu Trgovina muzičke opreme.';

Future<ScriptedClient> _pumpForm(
  WidgetTester tester, {
  required List<Map<String, dynamic>> stores,
  Future<http.Response>? storeResponse,
}) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final httpClient = ScriptedClient((request) {
    if (request.method == 'GET' &&
        request.url.path.endsWith('/admin/music-stores')) {
      if (storeResponse != null) {
        return storeResponse;
      }
      return jsonResponse({
        'items': stores,
        'page': 1,
        'pageSize': stores.length,
        'totalCount': stores.length,
      }, 200);
    }
    return jsonResponse(const {}, 200);
  });
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    httpClient: httpClient,
    tokenReader: () => fakeJwt(),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: httpClient,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<AuthState>.value(value: authState),
        ChangeNotifierProvider<AdminUserProvider>.value(
          value: AdminUserProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<MusicStoreProvider>.value(
          value: MusicStoreProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(home: UserProvisionFormScreen()),
    ),
  );
  await tester.pumpAndSettle();
  return httpClient;
}

Future<void> _pickRole(WidgetTester tester, UserRole role) async {
  await tester.tap(find.byType(DropdownButtonFormField<UserRole>));
  await tester.pumpAndSettle();
  await tester.tap(find.text(userRoleLabel(role)).last);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
    'selecting StoreEmployee with an empty store list shows the message and '
    'sets the role back to null',
    (WidgetTester tester) async {
      await _pumpForm(tester, stores: const []);

      await _pickRole(tester, UserRole.storeEmployee);

      expect(find.text(_message), findsOneWidget);
      final roleDropdown = tester.widget<DropdownButton<UserRole>>(
        find.byType(DropdownButton<UserRole>),
      );
      expect(roleDropdown.value, isNull);
      expect(find.byType(SwitchListTile), findsNothing);
    },
  );

  testWidgets('selecting Instructor never calls the store check', (
    WidgetTester tester,
  ) async {
    final httpClient = await _pumpForm(tester, stores: const []);

    await _pickRole(tester, UserRole.instructor);

    expect(find.text(_message), findsNothing);
    final storeRequests = httpClient.getUrls.where(
      (url) => url.contains('admin/music-stores'),
    );
    expect(storeRequests, isEmpty);
    final roleDropdown = tester.widget<DropdownButton<UserRole>>(
      find.byType(DropdownButton<UserRole>),
    );
    expect(roleDropdown.value, UserRole.instructor);
  });

  testWidgets(
    'selecting StoreEmployee with one store keeps the role and shows the '
    'store dropdown',
    (WidgetTester tester) async {
      await _pumpForm(
        tester,
        stores: const [
          {'id': 1, 'storeName': 'Trgovina A', 'businessHours': '09-20'},
        ],
      );

      await _pickRole(tester, UserRole.storeEmployee);

      final roleDropdown = tester.widget<DropdownButton<UserRole>>(
        find.byType(DropdownButton<UserRole>),
      );
      expect(roleDropdown.value, UserRole.storeEmployee);
      expect(find.text(_message), findsNothing);
      expect(find.text('Prodavnica'), findsOneWidget);
    },
  );

  testWidgets(
    'a slow empty store check does not reset a role that the user changed '
    'meanwhile',
    (WidgetTester tester) async {
      final storeResponseCompleter = Completer<http.Response>();

      await _pumpForm(
        tester,
        stores: const [],
        storeResponse: storeResponseCompleter.future,
      );

      await tester.tap(find.byType(DropdownButtonFormField<UserRole>));
      await tester.pumpAndSettle();
      await tester.tap(find.text(userRoleLabel(UserRole.storeEmployee)).last);
      await tester.pump();

      await tester.tap(find.byType(DropdownButtonFormField<UserRole>));
      await tester.pump();
      await tester.tap(find.text(userRoleLabel(UserRole.instructor)).last);
      await tester.pump();

      storeResponseCompleter.complete(
        jsonResponse({
          'items': const [],
          'page': 1,
          'pageSize': 0,
          'totalCount': 0,
        }, 200),
      );
      await tester.pumpAndSettle();

      final roleDropdown = tester.widget<DropdownButton<UserRole>>(
        find.byType(DropdownButton<UserRole>),
      );
      expect(roleDropdown.value, UserRole.instructor);
    },
  );

  testWidgets('a successful save resets the role dropdown to empty', (
    WidgetTester tester,
  ) async {
    await _pumpForm(tester, stores: const []);

    await _pickRole(tester, UserRole.instructor);

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Korisničko ime'),
      'korisnik',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Email'),
      'korisnik@mail.com',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Lozinka'),
      'Lozinka123!',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Ime *'),
      'Ime',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Prezime *'),
      'Prezime',
    );
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    final roleDropdown = tester.widget<DropdownButton<UserRole>>(
      find.byType(DropdownButton<UserRole>),
    );
    expect(roleDropdown.value, isNull);
  });
}
