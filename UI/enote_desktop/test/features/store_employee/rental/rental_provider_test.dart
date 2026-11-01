import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';

import '../../../helpers.dart';

Map<String, dynamic> _rentalJson([Map<String, dynamic> extra = const {}]) => {
  'id': 7,
  'instrumentId': 1,
  'musicStoreId': 1,
  'studentProfileId': 1,
  'studentUserId': 2,
  'studentName': 'Student Test',
  'instrumentModel': 'Stratocaster',
  'instrumentType': 'Gitara',
  'storeName': 'Shop',
  'rentalStatus': 'Approved',
  'requestedAt': DateTime(2026, 1, 5).toIso8601String(),
  'fee': 10.0,
  'totalFee': 5.0,
  'isPaid': false,
  ...extra,
};

RentalProvider _provider(ScriptedClient client) {
  final apiClient = ApiClient(
    baseUrl: 'http://localhost/',
    authState: AuthState(
      baseUrl: 'http://localhost/',
      httpClient: client,
      tokenReader: () => fakeJwt(username: 'test', role: 'StoreEmployee'),
    ),
    httpClient: client,
  );
  return RentalProvider(apiClient: apiClient);
}

void main() {
  group('RentalProvider.transition', () {
    const paths = {
      RentalTrigger.approve: 'approve',
      RentalTrigger.reject: 'reject',
      RentalTrigger.pickup: 'pickup',
      RentalTrigger.complete: 'complete',
      RentalTrigger.returnEarly: 'return-early',
      RentalTrigger.cancel: 'cancel',
    };

    for (final entry in paths.entries) {
      test('transition POSTs to the path of ${entry.key}', () async {
        final client = ScriptedClient((_) => jsonResponse(_rentalJson(), 200));

        await _provider(client).transition(7, entry.key);

        final posted = client.recorded.where((r) => r.method == 'POST').single;
        expect(
          Uri.parse(posted.url).path,
          endsWith('/shop/rentals/7/${entry.value}'),
        );
      });
    }

    test('transition sends no body for an empty note', () async {
      final client = ScriptedClient((_) => jsonResponse(_rentalJson(), 200));

      await _provider(client).transition(7, RentalTrigger.cancel, note: '  ');

      expect(
        client.recorded.where((r) => r.method == 'POST').single.body,
        isNull,
      );
    });

    test('transition returns the updated rental', () async {
      final client = ScriptedClient(
        (_) => jsonResponse(_rentalJson({'rentalStatus': 'Active'}), 200),
      );

      final updated = await _provider(client)
          .transition(7, RentalTrigger.pickup);

      expect(updated.id, 7);
      expect(updated.rentalStatus, InstrumentRentalStatus.active);
    });
  });
}
