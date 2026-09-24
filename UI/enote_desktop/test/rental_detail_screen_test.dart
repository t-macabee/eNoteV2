import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_detail_screen.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';

import 'helpers.dart';

void main() {
  testWidgets(
      'RentalDetailScreen renders refund date row when refundedAt is null',
      (tester) async {
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
      return jsonResponse({
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
        }, 200);
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

    expect(find.text('Datum povraćaja'), findsOneWidget);
  });
}
