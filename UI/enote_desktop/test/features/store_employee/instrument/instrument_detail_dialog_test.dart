import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_detail_dialog.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/theme/app_theme.dart';

import '../../../helpers.dart';

void main() {
  testWidgets(
    'the instrument dialog shows every fact without scrolling at the start window size',
    (tester) async {
      tester.view.physicalSize = const Size(1280, 720);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final client = ScriptedClient((_) => jsonResponse(const {}, 404));
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(),
        httpClient: client,
      );
      final instrument = InstrumentDto.fromJson(const {
        'id': 1,
        'musicStoreId': 10,
        'model': 'Katana-100 MkII',
        'manufacturerId': 1,
        'manufacturerName': 'Boss',
        'instrumentTypeId': 2,
        'instrumentType': 'Dodatna oprema',
        'description': 'Moderno digitalno pojačalo koje kombinuje veliku snagu sa nevjerovatnom svestranošću.',
        'monthlyFee': 45.0,
        'isAvailable': true,
        'imagePath': '/api/v1/uploads/instruments/katana.webp',
      });

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            Provider<ApiClient>.value(value: apiClient),
            ChangeNotifierProvider(
              create: (_) => InstrumentProvider(apiClient: apiClient),
            ),
          ],
          child: MaterialApp(
            theme: desktopTheme,
            home: Scaffold(
              body: Builder(
                builder: (context) => TextButton(
                  onPressed: () =>
                      InstrumentDetailDialog.show(context, instrument),
                  child: const Text('Otvori'),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('Otvori'));
      await tester.pumpAndSettle();

      final panel = tester.getRect(
        find
            .descendant(
              of: find.byType(Dialog),
              matching: find.byType(Material),
            )
            .first,
      );
      final lastFact = tester.getRect(find.text('Dostupan'));

      expect(lastFact.bottom, lessThanOrEqualTo(panel.bottom));
      expect(find.text('Uredi'), findsOneWidget);
      expect(find.text('45.00 KM'), findsOneWidget);
    },
  );
}
