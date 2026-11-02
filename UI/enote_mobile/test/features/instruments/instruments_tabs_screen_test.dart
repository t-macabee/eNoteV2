import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/instruments/instrument_provider.dart';
import 'package:enote_mobile/features/instruments/instruments_tabs_screen.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

void main() {
  testWidgets('Instrumenti tab shows Katalog and Iznajmljivanja', (
    tester,
  ) async {
    setPhoneSurface(tester);
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode({'items': [], 'page': 1, 'pageSize': 20, 'totalCount': 0}),
        200,
      ),
    );
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
        child: MaterialApp(
          theme: mobileTheme,
          home: const Scaffold(body: InstrumentsTabsScreen()),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Katalog'), findsOneWidget);
    expect(find.text('Iznajmljivanja'), findsOneWidget);
  });
}
