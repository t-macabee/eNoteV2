import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_form_screen.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_provider.dart';

import '../../../helpers.dart';

Future<ScriptedClient> _pumpForm(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final client = ScriptedClient((request) {
    if (request.method == 'POST') {
      return jsonResponse(const {
        'id': 1,
        'type': 'Gitara',
        'monthlyFee': 30,
      }, 200);
    }
    return jsonResponse('', 200);
  });

  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
    httpClient: client,
  );

  await tester.pumpWidget(
    ChangeNotifierProvider<InstrumentTypeProvider>(
      create: (_) => InstrumentTypeProvider(apiClient: apiClient),
      child: const MaterialApp(home: InstrumentTypeFormScreen()),
    ),
  );
  await tester.pumpAndSettle();
  return client;
}

Future<void> _submitFee(WidgetTester tester, String fee) async {
  await tester.enterText(find.widgetWithText(TextFormField, 'Tip'), 'Gitara');
  await tester.enterText(
    find.widgetWithText(TextFormField, 'Mjesečna naknada'),
    fee,
  );
  await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('empty fee shows the instrument fee message', (
    WidgetTester tester,
  ) async {
    await _pumpForm(tester);

    await tester.enterText(find.widgetWithText(TextFormField, 'Tip'), 'Gitara');

    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(
      find.text('Unesite mjesečnu naknadu u KM (npr. 45.00).'),
      findsOneWidget,
    );
  });

  testWidgets(
    'fee below the minimum shows the minimum message and sends no POST',
    (WidgetTester tester) async {
      final client = await _pumpForm(tester);

      await _submitFee(tester, '29.99');

      expect(find.text('Vrijednost mora biti najmanje 30.'), findsOneWidget);
      expect(client.postUrls, isEmpty);
    },
  );

  testWidgets(
    'fee with too many decimals shows the decimals message and sends no POST',
    (WidgetTester tester) async {
      final client = await _pumpForm(tester);

      await _submitFee(tester, '45.999');

      expect(
        find.text('Vrijednost može imati najviše 2 decimale.'),
        findsOneWidget,
      );
      expect(client.postUrls, isEmpty);
    },
  );

  testWidgets('valid fee sends one POST', (WidgetTester tester) async {
    final client = await _pumpForm(tester);

    await _submitFee(tester, '30');

    expect(client.postUrls, hasLength(1));
  });
}
