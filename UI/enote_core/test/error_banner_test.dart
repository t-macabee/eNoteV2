import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/theme/app_theme.dart';

void main() {
  testWidgets('ErrorBanner renders a danger StatusBanner', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        home: const Scaffold(body: ErrorBanner(message: 'Greška')),
      ),
    );

    final banner = tester.widget<StatusBanner>(find.byType(StatusBanner));
    expect(banner.tone, StatusTone.danger);
    expect(banner.message, 'Greška');
    expect(banner.icon, Icons.error_outline);
  });

  testWidgets('ErrorBanner.show uses the error color', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        home: const Scaffold(body: SizedBox()),
      ),
    );

    ErrorBanner.show(
      tester.element(find.byType(Scaffold)),
      message: 'Neuspješno',
    );
    await tester.pump();

    final snackBar = tester.widget<SnackBar>(find.byType(SnackBar));
    expect(snackBar.backgroundColor, AppTheme.error);
    expect(find.text('Neuspješno'), findsOneWidget);
  });
}
