import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/payments/payment_polling.dart';

class _Item {
  final PaymentStatus status;

  const _Item(this.status);
}

void main() {
  test('fetch throws once then returns succeeded -> returns succeeded',
      () async {
    var calls = 0;
    final result = await pollUntilSucceeded<_Item>(
      () async {
        calls++;
        if (calls == 1) throw Exception('timeout');
        return const _Item(PaymentStatus.succeeded);
      },
      (p) => p.status,
      interval: Duration.zero,
    );

    expect(result?.status, PaymentStatus.succeeded);
  });

  test('fetch throws every time -> throws', () async {
    await expectLater(
      pollUntilSucceeded<_Item>(
        () async => throw Exception('offline'),
        (p) => p.status,
        attempts: 3,
        interval: Duration.zero,
      ),
      throwsException,
    );
  });

  test('fetch throws then returns pending -> returns pending, no throw',
      () async {
    var calls = 0;
    final result = await pollUntilSucceeded<_Item>(
      () async {
        calls++;
        if (calls == 1) throw Exception('timeout');
        return const _Item(PaymentStatus.requiresAction);
      },
      (p) => p.status,
      interval: Duration.zero,
    );

    expect(result?.status, PaymentStatus.requiresAction);
  });
}
