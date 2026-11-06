import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/main.dart';

import 'helpers.dart';

void main() {
  testWidgets('a message shown from a dialog stays above the dialog barrier', (
    tester,
  ) async {
    final client = ScriptedClient((_) => jsonResponse({}, 200));
    final authState = AuthState(tokenReader: () => null, httpClient: client);
    final navigatorKey = GlobalKey<NavigatorState>();
    await tester.pumpWidget(
      MyApp(
        authState: authState,
        apiClient: ApiClient(
          baseUrl: 'http://test/api/v1/',
          authState: authState,
          httpClient: client,
        ),
        navigatorKey: navigatorKey,
      ),
    );
    await tester.pumpAndSettle();

    var dismissed = false;
    showDialog<void>(
      context: navigatorKey.currentContext!,
      barrierDismissible: false,
      builder: (_) => AlertDialog(
        content: Builder(
          builder: (context) => TextButton(
            onPressed: () => ErrorBanner.show(
              context,
              message: 'Poruka',
              autoDismiss: false,
              onDismissed: () => dismissed = true,
            ),
            child: const Text('prikaži'),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('prikaži'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Zatvori'), warnIfMissed: false);
    await tester.pump();

    expect(dismissed, isTrue);
  });
}
