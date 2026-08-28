import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';
import 'package:enote_desktop/features/admin/users/user_provision_form_screen.dart';
import 'package:enote_desktop/features/admin/users/user_provision_service.dart';

/// Answers GETs on the music-stores endpoint with a single store and POSTs on
/// admin/users with a minimal userId payload, recording every POSTed body so
/// the form's requests can be asserted without a real backend.
class _ProvisioningHttpClient extends http.BaseClient {
  final List<String?> postedBodies = [];
  int _nextUserId = 1;

  static const _storesJson =
      '{"items":[{"id":1,"storeName":"Trgovina A","businessHours":"09-20"}],'
      '"page":1,"pageSize":100,"totalCount":1}';

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request.method == 'GET') {
      final bytes = utf8.encode(_storesJson);
      return http.StreamedResponse(
        Stream.value(bytes),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    final body = request is http.Request ? request.body : null;
    postedBodies.add(body);

    final responseJson = jsonEncode({'userId': _nextUserId++});
    final bytes = utf8.encode(responseJson);
    return http.StreamedResponse(
      Stream.value(bytes),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets(
    'provisioning a second StoreEmployee without touching the store '
    "dropdown does not resend the first employee's store",
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(800, 1600);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final httpClient = _ProvisioningHttpClient();
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: httpClient,
      );

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            Provider<UserProvisionService>.value(
              value: UserProvisionService(apiClient: apiClient),
            ),
            ChangeNotifierProvider<MusicStoreProvider>.value(
              value: MusicStoreProvider(apiClient: apiClient),
            ),
          ],
          child: const MaterialApp(home: UserProvisionFormScreen()),
        ),
      );

      Future<void> fillBasicFields(String username) async {
        await tester.enterText(
          find.widgetWithText(TextFormField, 'Korisničko ime'),
          username,
        );
        await tester.enterText(
          find.widgetWithText(TextFormField, 'Email'),
          '$username@mail.com',
        );
        await tester.enterText(
          find.widgetWithText(TextFormField, 'Lozinka'),
          'Lozinka123!',
        );
      }

      Future<void> pickRole(UserRole role) async {
        await tester.tap(find.byType(DropdownButtonFormField<UserRole>));
        await tester.pumpAndSettle();
        await tester.tap(find.text(role.label).last);
        await tester.pumpAndSettle();
      }

      Future<void> pickStore(String storeName) async {
        await tester.tap(find.byType(DropdownButtonFormField<Object>));
        await tester.pumpAndSettle();
        await tester.tap(find.text(storeName).last);
        await tester.pumpAndSettle();
      }

      Future<void> save() async {
        await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
        await tester.pumpAndSettle();
      }

      // First provision: StoreEmployee with store A.
      await fillBasicFields('prvi');
      await pickRole(UserRole.storeEmployee);
      await pickStore('Trgovina A');
      await save();

      expect(httpClient.postedBodies, hasLength(1));
      final firstBody =
          jsonDecode(httpClient.postedBodies[0]!) as Map<String, dynamic>;
      expect(firstBody['role'], 'StoreEmployee');
      expect(firstBody['musicStoreId'], 1,
          reason: 'the first provision must send the selected store');

      // Second provision: only re-pick the role — the store dropdown is left
      // untouched, exactly like the repro. After the reset fix the store
      // required-dropdown is empty, so the submit must be blocked.
      await fillBasicFields('drugi');
      await pickRole(UserRole.storeEmployee);
      await save();

      expect(
        find.text('Prodavnica je obavezna za StoreEmployee.'),
        findsOneWidget,
        reason: 'the reset store dropdown must block the second submit',
      );
      expect(httpClient.postedBodies, hasLength(1),
          reason: 'no stale musicStoreId may reach a second POST');
    },
  );
}
