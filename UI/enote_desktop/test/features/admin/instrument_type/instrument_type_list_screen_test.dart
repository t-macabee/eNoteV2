import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_list_screen.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_provider.dart';
import 'package:enote_desktop/widgets/entity_list_config.dart';

import '../../../helpers.dart';

void main() {
  testWidgets('the instrument type list shows the monthly fee in KM', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final client = ScriptedClient(
      (_) => jsonResponse(const {
        'items': [
          {'id': 1, 'type': 'Limeni', 'monthlyFee': 55.0},
        ],
        'page': 1,
        'pageSize': 20,
        'totalCount': 1,
      }, 200),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: client,
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<InstrumentTypeProvider>(
        create: (_) => InstrumentTypeProvider(apiClient: apiClient),
        child: const MaterialApp(
          home: Scaffold(
            body: InstrumentTypeListScreen(listStyle: EntityListStyle.table),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('55.00 KM'), findsOneWidget);
  });
}
