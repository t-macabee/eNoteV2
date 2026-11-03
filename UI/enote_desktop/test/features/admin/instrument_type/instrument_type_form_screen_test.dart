import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_form_screen.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_provider.dart';

import '../../../helpers.dart';

void main() {
  testWidgets('empty fee shows the instrument fee message', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: ScriptedClient((_) => jsonResponse('', 200)),
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<InstrumentTypeProvider>(
        create: (_) => InstrumentTypeProvider(apiClient: apiClient),
        child: const MaterialApp(home: InstrumentTypeFormScreen()),
      ),
    );
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextFormField, 'Tip'), 'Gitara');

    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(
      find.text('Unesite mjesečnu naknadu u KM (npr. 45.00).'),
      findsOneWidget,
    );
  });
}
