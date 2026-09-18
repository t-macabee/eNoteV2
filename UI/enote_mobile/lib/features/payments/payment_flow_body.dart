import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import 'payment_flow_controller.dart';
import 'payment_status_view.dart';

/// Chrome shared by both payment screens: rebuilds [body] on [controller], blocks
/// back navigation while processing, and shows a close action only in the
/// states a user may leave from (A, E, F).
class PaymentFlowScaffold extends StatelessWidget {
  final PaymentFlowController controller;
  final String title;
  final WidgetBuilder body;

  const PaymentFlowScaffold({
    super.key,
    required this.controller,
    required this.title,
    required this.body,
  });

  static const _closable = {
    PaymentView.review,
    PaymentView.failed,
    PaymentView.pending,
  };

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) => PopScope(
        canPop: controller.view != PaymentView.processing,
        child: Scaffold(
          appBar: AppBar(
            title: Text(title),
            actions: [
              if (_closable.contains(controller.view))
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.of(context).maybePop(),
                ),
            ],
          ),
          body: body(context),
        ),
      ),
    );
  }
}

/// States A–F of the payment flow, shared by the tuition and rental screens.
///
/// The review body (state A) stays with the calling screen and arrives as
/// [reviewChild]; every later state is a [PaymentStatusView] driven by
/// [controller].
class PaymentFlowBody extends StatelessWidget {
  final PaymentFlowController controller;
  final Widget reviewChild;

  /// Optional D-state note; rentals keep the [PaymentStatusView] default.
  final String? successNote;

  /// D-state back button label and action, screen-specific.
  final String successCancelLabel;
  final VoidCallback onSuccessBack;

  const PaymentFlowBody({
    super.key,
    required this.controller,
    required this.reviewChild,
    required this.successCancelLabel,
    required this.onSuccessBack,
    this.successNote,
  });

  @override
  Widget build(BuildContext context) {
    return switch (controller.view) {
      PaymentView.review => reviewChild,
      PaymentView.processing => const PaymentStatusView(
        state: PaymentFlowState.processing,
        cancelLabel: 'Nazad',
        onCancel: null,
      ),
      PaymentView.succeeded => PaymentStatusView(
        state: PaymentFlowState.succeeded,
        detail: _successDetail(),
        successNote: successNote,
        cancelLabel: successCancelLabel,
        onCancel: onSuccessBack,
      ),
      PaymentView.failed => PaymentStatusView(
        state: PaymentFlowState.failed,
        detail: controller.failureMessage,
        retryLabel: 'Pokušaj ponovo',
        onRetry: controller.isBusy ? null : controller.attempt,
        isBusy: controller.isBusy,
        cancelLabel: 'Odustani',
        onCancel: () => Navigator.of(context).maybePop(),
      ),
      PaymentView.pending => PaymentStatusView(
        state: PaymentFlowState.pending,
        retryLabel: 'Provjeri ponovo',
        onRetry: controller.isPolling ? null : controller.poll,
        isBusy: controller.isPolling,
        cancelLabel: 'Nazad',
        onCancel: () => Navigator.of(context).maybePop(),
      ),
    };
  }

  String? _successDetail() {
    final payment = controller.payment;
    if (payment == null) return null;
    return formatPaymentDetail(
      payment.amountCents,
      payment.paidAt,
      currency: payment.currency,
    );
  }
}
