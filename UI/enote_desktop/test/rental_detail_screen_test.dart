import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_detail_screen.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';

import 'helpers.dart';

Future<void> _pumpScreen(WidgetTester tester, Map<String, dynamic> rental) async {
  tester.view.physicalSize = const Size(1400, 2000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final client = ScriptedClient((request) {
    final path = request.url.path;
    if (path.endsWith('/payments')) {
      return jsonResponse({
        'id': 9,
        'rentalId': 5,
        'paymentIntentId': 'pi_1',
        'amountCents': 1000,
        'currency': 'bam',
        'status': 'PartiallyRefunded',
        'refundedCents': 100,
        'refundedAt': null,
      }, 200);
    }
    return jsonResponse(rental, 200);
  });

  final authState = AuthState(
    baseUrl: 'http://localhost/',
    httpClient: client,
    tokenReader: () => fakeJwt(username: 'test', role: 'StoreEmployee'),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost/',
    authState: authState,
    httpClient: client,
  );
  final provider = RentalProvider(apiClient: apiClient);

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<RentalProvider>.value(value: provider),
      ],
      child: const MaterialApp(home: RentalDetailScreen(rentalId: 5)),
    ),
  );

  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
      'RentalDetailScreen renders refund date row when refundedAt is null',
      (tester) async {
    await _pumpScreen(tester, {
      'id': 5,
      'instrumentId': 1,
      'musicStoreId': 1,
      'studentProfileId': 1,
      'studentUserId': 1,
      'instrumentModel': 'Fender',
      'instrumentType': 'Gitara',
      'storeName': 'Shop',
      'rentalStatus': 'Active',
      'requestedAt': DateTime.now().toIso8601String(),
      'fee': 10.0,
      'isPaid': true,
      'amountPaid': 10.0,
      'paidAt': DateTime.now().toIso8601String(),
    });

    expect(find.text('Datum povraćaja'), findsOneWidget);
  });

  testWidgets('RentalDetailScreen shows no database ids', (tester) async {
    final now = DateTime.now().toIso8601String();
    await _pumpScreen(tester, {
      'id': 5,
      'instrumentId': 1,
      'musicStoreId': 1,
      'studentProfileId': 1,
      'studentUserId': 1,
      'instrumentModel': 'Fender',
      'instrumentType': 'Gitara',
      'storeName': 'Shop',
      'rentalStatus': 'Active',
      'requestedAt': now,
      'approvedAt': now,
      'approvedById': 42,
      'rejectedAt': now,
      'rejectedById': 43,
      'fee': 10.0,
      'isPaid': true,
      'amountPaid': 10.0,
      'paidAt': now,
    });

    expect(find.text('Iznajmljivanje — Fender'), findsOneWidget);
    expect(find.text('Nepoznat korisnik'), findsOneWidget);
    expect(find.textContaining('#'), findsNothing);
    expect(find.text('Odobreno'), findsOneWidget);
    expect(find.text('Odbijeno'), findsOneWidget);
  });
}
