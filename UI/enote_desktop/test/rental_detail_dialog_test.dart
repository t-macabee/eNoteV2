import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_detail_dialog.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';

import 'helpers.dart';

Future<void> _pumpDialog(
  WidgetTester tester,
  Map<String, dynamic> rental, {
  void Function(Map<String, dynamic> rental)? onApprove,
}) async {
  tester.view.physicalSize = const Size(1400, 2000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final current = Map<String, dynamic>.of(rental);
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
    if (request.method == 'POST' && path.endsWith('/approve')) {
      onApprove?.call(current);
      return jsonResponse(current, 200);
    }
    return jsonResponse(current, 200);
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
      child: MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => TextButton(
              onPressed: () => RentalDetailDialog.show(context, 5),
              child: const Text('Otvori'),
            ),
          ),
        ),
      ),
    ),
  );

  await tester.tap(find.text('Otvori'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
    'RentalDetailDialog renders refund date row when refundedAt is null',
    (tester) async {
      await _pumpDialog(tester, {
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
    },
  );

  testWidgets('RentalDetailDialog shows no database ids', (tester) async {
    final now = DateTime.now().toIso8601String();
    await _pumpDialog(tester, {
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

    expect(find.text('Iznajmljivanje - Fender'), findsOneWidget);
    expect(find.text('Nepoznat korisnik'), findsOneWidget);
    expect(find.textContaining('#'), findsNothing);
    expect(find.text('Odobreno'), findsOneWidget);
    expect(find.text('Odbijeno'), findsOneWidget);
  });

  testWidgets('the close button closes the dialog', (tester) async {
    await _pumpDialog(tester, {
      'id': 5,
      'instrumentModel': 'Fender',
      'instrumentType': 'Gitara',
      'storeName': 'Shop',
      'rentalStatus': 'Active',
      'requestedAt': DateTime.now().toIso8601String(),
      'fee': 10.0,
      'isPaid': false,
    });

    expect(find.byType(RentalDetailDialog), findsOneWidget);
    expect(find.byTooltip('Zatvori'), findsOneWidget);

    await tester.tap(find.byTooltip('Zatvori'));
    await tester.pumpAndSettle();

    expect(find.byType(RentalDetailDialog), findsNothing);
  });

  testWidgets('approving a pending rental shows the specific success message', (
    tester,
  ) async {
    await _pumpDialog(
      tester,
      {
        'id': 5,
        'instrumentId': 1,
        'musicStoreId': 1,
        'studentProfileId': 1,
        'studentUserId': 1,
        'instrumentModel': 'Fender',
        'instrumentType': 'Gitara',
        'storeName': 'Shop',
        'rentalStatus': 'Pending',
        'requestedAt': DateTime.now().toIso8601String(),
        'fee': 10.0,
        'isPaid': false,
      },
      onApprove: (rental) {
        rental['rentalStatus'] = 'Approved';
        rental['approvedAt'] = DateTime.now().toIso8601String();
      },
    );

    await tester.tap(find.widgetWithText(ElevatedButton, 'Odobri'));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(find.text('Zahtjev za iznajmljivanje je odobren.'), findsOneWidget);
  });
}
