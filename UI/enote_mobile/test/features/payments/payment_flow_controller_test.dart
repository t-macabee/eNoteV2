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
}) {
  return PaymentFlowController(
    gateway: gateway,
    isConfigured: isConfigured,
    createIntent: () async => 'pi_test_123_secret_abc',
    status: () async => status == null ? null : _row(status),
    pollUntilSucceeded: () async {
      if (pollError != null) throw pollError;
      return pollResult;
    },
  );
}

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

void main() {
  test('a cancelled sheet with a failed row shows the declined copy', () async {
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetCancelled());
    final controller = _controller(
      gateway: gateway,
      status: PaymentStatus.failed,
    );

    await controller.attempt();

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

      await controller.attempt();

      expect(controller.view, PaymentView.failed);
      expect(controller.failureMessage, 'Plaćanje je otkazano.');
    },
  );

  test('a sheet failure shows the Stripe message', () async {
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetFailure('Your card was declined.'));
    final controller = _controller(gateway: gateway);

    await controller.attempt();

    expect(controller.view, PaymentView.failed);
    expect(controller.failureMessage, 'Your card was declined.');
    expect(controller.isBusy, isFalse);
  });

  test('a poll that never succeeds lands on pending', () async {
    final controller = _controller(
      gateway: FakePaymentSheetGateway(),
      pollResult: null,
    );

    await controller.attempt();

    expect(controller.view, PaymentView.pending);
    expect(controller.payment, isNull);
    expect(controller.isPolling, isFalse);
  });

  test('a poll error after a successful sheet lands on pending', () async {
    final controller = _controller(
      gateway: FakePaymentSheetGateway(),
      pollError: ApiException('Network error'),
    );

    await controller.attempt();

    expect(controller.view, PaymentView.pending);
    expect(controller.failureMessage, isNull);
    expect(controller.isPolling, isFalse);
  });

  test(
    'a retry after a declined sheet does not carry the old message into pending',
    () async {
      final gateway = FakePaymentSheetGateway()
        ..queuePresent(PaymentSheetCancelled());
      final controller = _controller(
        gateway: gateway,
        status: PaymentStatus.failed,
        pollError: ApiException('Network error'),
      );

      await controller.attempt();

      expect(controller.view, PaymentView.failed);
      expect(controller.failureMessage, PaymentFlowController.declinedMessage);

      await controller.attempt();

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

    await controller.attempt();

    expect(controller.view, PaymentView.succeeded);
    expect(controller.payment, summary);
  });
}
