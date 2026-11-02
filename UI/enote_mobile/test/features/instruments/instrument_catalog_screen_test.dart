import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/instruments/instrument_catalog_screen.dart';
import 'package:enote_mobile/features/instruments/instrument_provider.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

const _debtErrorCopy = 'Stanje dugovanja trenutno nije moguće provjeriti.';

Future<ScriptedClient> _pumpCatalog(
  WidgetTester tester, {
  required bool Function() debtFails,
}) async {
  tester.view.physicalSize = const Size(400, 1600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);

  final client = ScriptedClient((request) {
    final path = request.url.path;
    if (path.endsWith('/student/rentals/debt')) {
      if (debtFails()) {
        return jsonResponse({
          'code': 'error.internal',
          'message': 'Greška na serveru.',
        }, 500);
      }
      return jsonResponse({'hasUnpaidDebt': false}, 200);
    }
    if (path.endsWith('/student/instruments/recommended')) {
      return jsonResponse(const [], 200);
    }
    if (path.endsWith('/instruments/public')) {
      return jsonResponse({
        'items': const [],
        'page': 1,
        'pageSize': 20,
        'totalCount': 0,
      }, 200);
    }
    return jsonResponse('', 204);
  });

  final authState = AuthState(
    baseUrl: _baseUrl,
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  final apiClient = ApiClient(
    baseUrl: _baseUrl,
    authState: authState,
    httpClient: client,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<InstrumentProvider>(
          create: (_) => InstrumentProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<RentalProvider>(
          create: (_) => RentalProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(home: Scaffold(body: InstrumentCatalogScreen())),
    ),
  );
  await tester.pumpAndSettle();
  return client;
}

void main() {
  testWidgets('shows the debt error banner when the debt call fails', (
    tester,
  ) async {
    await _pumpCatalog(tester, debtFails: () => true);

    expect(find.text(_debtErrorCopy), findsOneWidget);
    expect(find.text('Pokušaj ponovo'), findsOneWidget);
  });

  testWidgets('retrying the debt call removes the error banner', (
    tester,
  ) async {
    var fails = true;
    await _pumpCatalog(tester, debtFails: () => fails);

    expect(find.text(_debtErrorCopy), findsOneWidget);

    fails = false;
    await tester.tap(find.text('Pokušaj ponovo'));
    await tester.pumpAndSettle();

    expect(find.text(_debtErrorCopy), findsNothing);
  });

  testWidgets('opens on Dostupni and sends IsAvailable', (tester) async {
    final client = await _pumpCatalog(tester, debtFails: () => false);

    final dostupni = tester.widget<ChoiceChip>(
      find.widgetWithText(ChoiceChip, 'Dostupni'),
    );
    expect(dostupni.selected, isTrue);
    final catalogRequests = client.requests.where(
      (r) => r.url.path.endsWith('/instruments/public'),
    );
    expect(
      catalogRequests.last.url.queryParameters['search.IsAvailable'],
      'true',
    );
  });

  testWidgets('the Svi tab sends no IsAvailable', (tester) async {
    final client = await _pumpCatalog(tester, debtFails: () => false);

    await tester.tap(find.text('Svi'));
    await tester.pumpAndSettle();

    final catalogRequests = client.requests.where(
      (r) => r.url.path.endsWith('/instruments/public'),
    );
    expect(
      catalogRequests.last.url.queryParameters,
      isNot(contains('search.IsAvailable')),
    );
  });
}
