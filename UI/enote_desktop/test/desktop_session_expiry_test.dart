import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/reference_data/reference_data_dialog.dart';
import 'package:enote_desktop/main.dart';
import 'package:enote_desktop/shell/login_screen.dart';
import 'package:enote_desktop/shell/master_screen.dart';

import 'helpers.dart';

class _ExpiryHttpClient extends http.BaseClient {
  bool return401 = false;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (return401 && !request.url.path.contains('/auth/')) {
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode({'message': 'Unauthorized'}))),
        401,
        headers: {'content-type': 'application/json'},
      );
    }
    final body = jsonEncode({
      'items': [],
      'page': 1,
      'pageSize': 10,
      'totalCount': 0,
    });
    return http.StreamedResponse(
      Stream.value(utf8.encode(body)),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets(
    'fake 401 on non-auth path with a dialog open pops to LoginScreen with no dialog',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 900);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockHttp = _ExpiryHttpClient();
      final navigatorKey = GlobalKey<NavigatorState>();
      late AuthState authState;
      final sessionHttp = SessionHttpClient(
        inner: mockHttp,
        timeout: const Duration(seconds: 20),
        onUnauthorized: () {
          navigatorKey.currentState?.popUntil((route) => route.isFirst);
          authState.logout();
        },
      );
      authState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: sessionHttp,
        tokenReader: () => fakeJwt(role: 'Administrator'),
      );
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: sessionHttp,
      );

      await tester.pumpWidget(MyApp(
        authState: authState,
        apiClient: apiClient,
        navigatorKey: navigatorKey,
      ));
      await tester.pumpAndSettle();

      expect(find.byType(MasterScreen), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);

      await tester.tap(find.text('Referentni podaci'));
      await tester.pumpAndSettle();

      expect(find.byType(ReferenceDataDialog), findsOneWidget);

      mockHttp.return401 = true;

      try {
        await apiClient.get('courses');
      } catch (_) {}

      await tester.pumpAndSettle();

      expect(find.byType(LoginScreen), findsOneWidget);
      expect(find.byType(ReferenceDataDialog), findsNothing);
      expect(find.byType(Dialog), findsNothing);
      expect(authState.isAuthenticated, isFalse);
    },
  );
}
