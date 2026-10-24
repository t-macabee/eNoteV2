import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../config.dart';
import '../../widgets/labeled_value.dart';
import '../payments/payment_flow_body.dart';
import '../payments/payment_flow_controller.dart';
import '../payments/payment_review.dart';
import '../payments/payment_sheet_gateway.dart';
import 'tuition_payment_provider.dart';

class TuitionPaymentScreen extends StatefulWidget {
  final int enrollmentId;
  final String courseName;
  final double price;
  final PaymentSheetGateway gateway;
  final String stripePublishableKey;

  TuitionPaymentScreen({
    super.key,
    required this.enrollmentId,
    required this.courseName,
    required this.price,
    PaymentSheetGateway? gateway,
    String? stripePublishableKey,
  }) : gateway = gateway ?? StripePaymentSheetGateway(),
       stripePublishableKey = stripePublishableKey ?? kStripePublishableKey;

  @override
  State<TuitionPaymentScreen> createState() => _TuitionPaymentScreenState();
}

class _TuitionPaymentScreenState extends State<TuitionPaymentScreen> {
  late final PaymentFlowController _flow;

  TuitionPaymentProvider get _payments =>
      context.read<TuitionPaymentProvider>();

  @override
  void initState() {
    super.initState();
    _flow = PaymentFlowController(
      gateway: widget.gateway,
      isConfigured: widget.stripePublishableKey.isNotEmpty,
      createIntent: () => _payments.createIntent(widget.enrollmentId),
      status: () => _payments.status(widget.enrollmentId),
      pollUntilSucceeded: () =>
          _payments.pollUntilSucceeded(widget.enrollmentId),
    );
  }

  @override
  void dispose() {
    _flow.dispose();
    super.dispose();
  }

  Future<void> _pay() async {
    if (_flow.isBusy) return;
    final intent = await _flow.prepare();
    if (intent == null || !mounted) return;
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrda plaćanja',
      message:
          'Platiti ${intent.amountLabel} za kurs "${widget.courseName}" '
          'putem Stripe-a?',
    );
    if (confirmed != true || !mounted) return;
    await _flow.confirmAndPresent();
  }

  void _backToCourse() {
    if (mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    return PaymentFlowScaffold(
      controller: _flow,
      title: 'Plaćanje školarine',
      body: (_) => PaymentFlowBody(
        controller: _flow,
        reviewChild: _review(),
        successNote: 'Školarina je plaćena.',
        successCancelLabel: 'Nazad na kurs',
        onSuccessBack: _backToCourse,
      ),
    );
  }

  Widget _review() {
    final amountLabel = _flow.prepared?.amountLabel ?? formatKM(widget.price);
    return PaymentReview(
      amountLabel: amountLabel,
      onPay: _pay,
      isBusy: _flow.isBusy,
      isConfigured: _flow.isConfigured,
      children: [
        Text(widget.courseName, style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 16),
        LabeledValue(label: 'Iznos', value: amountLabel),
        const LabeledValue(label: 'Trajanje pristupa', value: '30 dana'),
      ],
    );
  }
}
