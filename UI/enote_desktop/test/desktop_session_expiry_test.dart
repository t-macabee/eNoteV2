import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/reference_data/reference_data_dialog.dart';
import 'package:enote_desktop/main.dart';
import 'package:enote_desktop/shell/login_screen.dart';
import 'package:enote_desktop/shell/master_screen.dart';

import 'helpers.dart';

void main() {
  testWidgets(
    'fake 401 on non-auth path with a dialog open pops to LoginScreen with no dialog',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 900);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      var return401 = false;
      final mockHttp = ScriptedClient((request) {
        if (return401 && !request.url.path.contains('/auth/')) {
          return jsonResponse({'message': 'Unauthorized'}, 401);
        }
        return jsonResponse(const {
          'items': [],
          'page': 1,
          'pageSize': 10,
          'totalCount': 0,
        }, 200);
      });
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

      return401 = true;

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
