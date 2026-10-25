import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/payments/payment_flow_controller.dart';
import 'package:enote_mobile/features/payments/payment_sheet_gateway.dart';

import 'fake_payment_sheet_gateway.dart';

PaymentFlowController _controller({
  required FakePaymentSheetGateway gateway,
  PaymentStatus? status,
  RentalPaymentDto? pollResult,
  bool isConfigured = true,
  Object? pollError,
  PaymentIntentDraft? intent,
  Object? intentError,
  Object? statusError,
}) {
  return PaymentFlowController(
    gateway: gateway,
    isConfigured: isConfigured,
    createIntent: () async {
      if (intentError != null) throw intentError;
      return intent ?? _intent();
    },
    status: () async {
      if (statusError != null) throw statusError;
      return status == null ? null : _row(status);
    },
    pollUntilSucceeded: () async {
      if (pollError != null) throw pollError;
      return pollResult;
    },
  );
}

CreatePaymentIntentResponse _intent({
  String clientSecret = 'pi_test_123_secret_abc',
  int amountCents = 800,
  String currency = 'bam',
}) => CreatePaymentIntentResponse(
  rentalId: 1,
  paymentIntentId: 'pi_test_123',
  clientSecret: clientSecret,
  amountCents: amountCents,
  currency: currency,
  status: PaymentStatus.requiresAction,
);

RentalPaymentDto _row(
  PaymentStatus status, {
  int amountCents = 0,
  DateTime? paidAt,
}) => RentalPaymentDto(
  id: 1,
  rentalId: 1,
  paymentIntentId: 'pi_test',
  amountCents: amountCents,
  currency: 'bam',
  status: status,
  paidAt: paidAt,
);

Future<void> _prepareAndPresent(PaymentFlowController controller) async {
  await controller.prepare();
  await controller.confirmAndPresent();
}

void main() {
  test('prepare keeps the created intent and does not open the sheet', () async {
    final gateway = FakePaymentSheetGateway();
    final controller = _controller(gateway: gateway);

    final prepared = await controller.prepare();

    expect(prepared?.clientSecret, 'pi_test_123_secret_abc');
    expect(prepared?.amountCents, 800);
    expect(prepared?.currency, 'bam');
    expect(prepared?.amountLabel, '8.00 KM');
    expect(controller.prepared, same(prepared));
    expect(controller.view, PaymentView.review);
    expect(controller.isBusy, isFalse);
    expect(gateway.initCalls, 0);
    expect(gateway.presentCalls, 0);
  });

  test('confirmAndPresent presents the prepared client secret', () async {
    final gateway = FakePaymentSheetGateway();
    final controller = _controller(
      gateway: gateway,
      intent: _intent(clientSecret: 'pi_other_secret'),
    );

    await controller.prepare();
    await controller.confirmAndPresent();

    expect(gateway.initCalls, 1);
    expect(gateway.lastClientSecret, 'pi_other_secret');
    expect(gateway.presentCalls, 1);
  });

  test('confirmAndPresent without a prepared intent does nothing', () async {
    final gateway = FakePaymentSheetGateway();
    final controller = _controller(gateway: gateway);

    await controller.confirmAndPresent();

    expect(gateway.initCalls, 0);
    expect(gateway.presentCalls, 0);
    expect(controller.view, PaymentView.review);
  });

  test('a failed prepare shows the server message and presents nothing', () async {
    final gateway = FakePaymentSheetGateway();
    final controller = _controller(
      gateway: gateway,
      intentError: ApiException('Kurs je besplatan.'),
    );

    final prepared = await controller.prepare();

    expect(prepared, isNull);
    expect(controller.prepared, isNull);
    expect(controller.view, PaymentView.failed);
    expect(controller.failureMessage, 'Kurs je besplatan.');
    expect(controller.isBusy, isFalse);
    expect(gateway.presentCalls, 0);
  });

  test('retry drops the prepared intent and returns to the review', () async {
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetCancelled());
    final controller = _controller(
      gateway: gateway,
      status: PaymentStatus.failed,
    );

    await _prepareAndPresent(controller);
    expect(controller.view, PaymentView.failed);
    expect(controller.prepared, isNotNull);

    controller.retry();

    expect(controller.view, PaymentView.review);
    expect(controller.failureMessage, isNull);
    expect(controller.prepared, isNull);
    expect(gateway.presentCalls, 1);
  });

  test('a cancelled sheet with a failed row shows the declined copy', () async {
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetCancelled());
    final controller = _controller(
      gateway: gateway,
      status: PaymentStatus.failed,
    );

    await _prepareAndPresent(controller);

    expect(controller.view, PaymentView.failed);
    expect(controller.failureMessage, PaymentFlowController.declinedMessage);
    expect(controller.isBusy, isFalse);
    expect(controller.isPolling, isFalse);
  });

  test(
    'a cancelled sheet with a non-failed row keeps the cancel copy',
    () async {
      final gateway = FakePaymentSheetGateway()
        ..queuePresent(PaymentSheetCancelled());
      final controller = _controller(
        gateway: gateway,
        status: PaymentStatus.requiresAction,
      );

      await _prepareAndPresent(controller);

      expect(controller.view, PaymentView.failed);
      expect(controller.failureMessage, 'Plaćanje je otkazano.');
    },
  );

  test('a failing status check after cancel keeps the cancel copy and logs',
      () async {
    final logged = <String>[];
    final previousDebugPrint = debugPrint;
    debugPrint = (message, {wrapWidth}) => logged.add(message ?? '');
    addTearDown(() => debugPrint = previousDebugPrint);

    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetCancelled());
    final controller = _controller(
      gateway: gateway,
      statusError: ApiException('Network error'),
    );

    await _prepareAndPresent(controller);

    expect(controller.view, PaymentView.failed);
    expect(controller.failureMessage, 'Plaćanje je otkazano.');
    expect(logged, hasLength(1));
    expect(logged.single, contains('Network error'));
  });

  test('a sheet failure shows the Stripe message', () async {
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetFailure('Your card was declined.'));
    final controller = _controller(gateway: gateway);

    await _prepareAndPresent(controller);

    expect(controller.view, PaymentView.failed);
    expect(controller.failureMessage, 'Your card was declined.');
    expect(controller.isBusy, isFalse);
  });

  test('a poll that never succeeds lands on pending', () async {
    final controller = _controller(
      gateway: FakePaymentSheetGateway(),
      pollResult: null,
    );

    await _prepareAndPresent(controller);

    expect(controller.view, PaymentView.pending);
    expect(controller.payment, isNull);
    expect(controller.isPolling, isFalse);
  });

  test('a poll error after a successful sheet lands on pending', () async {
    final controller = _controller(
      gateway: FakePaymentSheetGateway(),
      pollError: ApiException('Network error'),
    );

    await _prepareAndPresent(controller);

    expect(controller.view, PaymentView.pending);
    expect(controller.failureMessage, isNull);
    expect(controller.isPolling, isFalse);
  });

  test(
    'a second payment after a declined sheet does not carry the old message into pending',
    () async {
      final gateway = FakePaymentSheetGateway()
        ..queuePresent(PaymentSheetCancelled());
      final controller = _controller(
        gateway: gateway,
        status: PaymentStatus.failed,
        pollError: ApiException('Network error'),
      );

      await _prepareAndPresent(controller);

      expect(controller.view, PaymentView.failed);
      expect(controller.failureMessage, PaymentFlowController.declinedMessage);

      await _prepareAndPresent(controller);

      expect(controller.view, PaymentView.pending);
      expect(controller.failureMessage, isNull);
    },
  );

  test('a succeeded poll carries the payment into view D', () async {
    final summary = _row(
      PaymentStatus.succeeded,
      amountCents: 800,
      paidAt: DateTime(2026, 9, 9, 12, 41),
    );
    final controller = _controller(
      gateway: FakePaymentSheetGateway(),
      pollResult: summary,
    );

    await _prepareAndPresent(controller);

    expect(controller.view, PaymentView.succeeded);
    expect(controller.payment, summary);
  });
}
