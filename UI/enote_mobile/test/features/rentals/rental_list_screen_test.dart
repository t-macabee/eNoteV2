import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/rentals/rental_list_screen.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

Map<String, dynamic> _rental({
  required String status,
  int? overdueDays,
  bool isPaid = false,
}) => {
  'id': 1,
  'instrumentId': 1,
  'musicStoreId': 1,
  'studentProfileId': 7,
  'studentUserId': 1,
  'instrumentModel': 'C40',
  'instrumentType': 'Klasična gitara',
  'storeName': 'Muzika d.o.o.',
  'instrumentIsActive': true,
  'rentalStatus': status,
  'requestedAt': '2026-09-01T10:00:00',
  'fee': 20,
  'dailyPenaltyFee': 0,
  'isPaid': isPaid,
  'overdueDays': overdueDays,
};

Future<ScriptedClient> _pumpRentals(
  WidgetTester tester, {
  Map<String, dynamic>? rental,
}) async {
  setPhoneSurface(tester);
  final client = ScriptedClient(
    (_) => jsonResponse(
      jsonEncode({
        'items': [?rental],
        'page': 1,
        'pageSize': 20,
        'totalCount': rental == null ? 0 : 1,
      }),
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
        ChangeNotifierProvider<RentalProvider>(
          create: (_) => RentalProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: mobileTheme,
        home: const Scaffold(body: RentalListScreen()),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return client;
}

void main() {
  testWidgets('rental row shows the overdue chip', (tester) async {
    await _pumpRentals(
      tester,
      rental: _rental(status: 'Active', overdueDays: 3),
    );

    expect(find.text('Kasni 3 dana'), findsOneWidget);
    final overdue = tester.widget<StatusChip>(
      find.widgetWithText(StatusChip, 'Kasni 3 dana'),
    );
    expect(overdue.tone, StatusTone.danger);
  });

  testWidgets('returned unpaid rental shows Nije plaćeno', (tester) async {
    await _pumpRentals(tester, rental: _rental(status: 'Completed'));

    expect(find.text('Nije plaćeno'), findsOneWidget);
    final unpaid = tester.widget<StatusChip>(
      find.widgetWithText(StatusChip, 'Nije plaćeno'),
    );
    expect(unpaid.tone, StatusTone.warning);
  });

  testWidgets('opens on Sva and sends no rentalStatus', (tester) async {
    final client = await _pumpRentals(tester);

    final sva = tester.widget<ChoiceChip>(
      find.widgetWithText(ChoiceChip, 'Sva'),
    );
    expect(sva.selected, isTrue);
    expect(
      client.requests.first.url.queryParameters,
      isNot(contains('rentalStatus')),
    );
  });

  testWidgets('a status tab sends rentalStatus', (tester) async {
    final client = await _pumpRentals(
      tester,
      rental: _rental(status: 'Completed'),
    );

    await tester.tap(
      find.text(rentalStatusLabel(InstrumentRentalStatus.active)),
    );
    await tester.pumpAndSettle();

    expect(
      client.requests.last.url.queryParameters['rentalStatus'],
      InstrumentRentalStatus.active.toJson(),
    );
  });

  testWidgets('status tabs fit in one horizontally scrollable row', (
    tester,
  ) async {
    await _pumpRentals(tester);

    expect(
      find.byWidgetPredicate(
        (w) =>
            w is SingleChildScrollView && w.scrollDirection == Axis.horizontal,
      ),
      findsOneWidget,
    );
    expect(
      find.byType(DropdownButtonFormField<InstrumentRentalStatus?>),
      findsNothing,
    );
  });
}
