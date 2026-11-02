import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../config.dart';
import '../../../widgets/async_state_view.dart';
import '../../../widgets/detail_actions.dart';
import '../../../widgets/detail_header.dart';
import '../../../widgets/labeled_value.dart';
import '../rental_provider.dart';
import '../../payments/payment_flow_body.dart';
import '../../payments/payment_flow_controller.dart';
import '../../payments/payment_review.dart';
import '../../payments/payment_sheet_gateway.dart';
import 'rental_payment_provider.dart';

class RentalPaymentScreen extends StatefulWidget {
  final int rentalId;
  final PaymentSheetGateway gateway;
  final String stripePublishableKey;

  RentalPaymentScreen({
    super.key,
    required this.rentalId,
    PaymentSheetGateway? gateway,
    String? stripePublishableKey,
  }) : gateway = gateway ?? StripePaymentSheetGateway(),
       stripePublishableKey = stripePublishableKey ?? kStripePublishableKey;

  @override
  State<RentalPaymentScreen> createState() => _RentalPaymentScreenState();
}

class _RentalPaymentScreenState extends State<RentalPaymentScreen> {
  InstrumentRentalDto? _rental;
  Object? _error;
  bool _isLoading = true;
  late final PaymentFlowController _flow;

  RentalPaymentProvider get _payments => context.read<RentalPaymentProvider>();

  @override
  void initState() {
    super.initState();
    _flow = PaymentFlowController(
      gateway: widget.gateway,
      isConfigured: widget.stripePublishableKey.isNotEmpty,
      createIntent: () => _payments.createIntent(widget.rentalId),
      status: () => _payments.status(widget.rentalId),
      pollUntilSucceeded: () => _payments.pollUntilSucceeded(widget.rentalId),
    );
    _load();
  }

  @override
  void dispose() {
    _flow.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final rental = await context.read<RentalProvider>().getById(
        widget.rentalId,
      );
      if (!mounted) return;
      setState(() {
        _rental = rental;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e;
        _isLoading = false;
      });
    }
  }

  Future<void> _pay() async {
    final rental = _rental;
    if (rental == null || _flow.isBusy) return;
    final intent = await _flow.prepare();
    if (intent == null || !mounted) return;
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrda plaćanja',
      message:
          'Platiti ${intent.amountLabel} za ${rental.instrumentModel} putem Stripe-a?',
    );
    if (confirmed != true || !mounted) return;
    await _flow.confirmAndPresent();
  }

  void _backToRental() {
    if (mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    final rental = _rental;
    return PaymentFlowScaffold(
      controller: _flow,
      title: 'Plaćanje iznajmljivanja',
      body: (_) => rental == null
          ? AsyncStateView(
              isLoading: _isLoading,
              error: _error,
              onRetry: _load,
              child: const SizedBox.shrink(),
            )
          : _body(rental),
    );
  }

  Widget _body(InstrumentRentalDto rental) {
    if (rental.isPaid) {
      return _refusal();
    }
    return PaymentFlowBody(
      controller: _flow,
      reviewChild: _review(rental),
      successNote: 'Iznajmljivanje je izmireno.',
      successCancelLabel: 'Nazad na iznajmljivanje',
      onSuccessBack: _backToRental,
    );
  }

  Widget _refusal() {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        const Padding(
          padding: EdgeInsets.all(AppSpacing.lg),
          child: StatusBanner(
            icon: Icons.check_circle_outline,
            message: 'Ovo iznajmljivanje je već plaćeno.',
            tone: StatusTone.success,
          ),
        ),
        DetailActions(
          actions: [
            OutlinedButton(
              onPressed: () => Navigator.of(context).maybePop(),
              child: const Text('Nazad'),
            ),
          ],
        ),
      ],
    );
  }

  Widget _review(InstrumentRentalDto rental) {
    final amountLabel =
        _flow.prepared?.amountLabel ?? formatKM(rental.totalFee);
    return PaymentReview(
      header: DetailHeader(
        leading: ImageThumbnail(
          imageUrl: rental.instrumentImagePath,
          apiClient: context.read<ApiClient>(),
          size: AppSize.headerThumbnail,
          borderRadius: AppRadius.md,
        ),
        title: rental.instrumentModel,
        subtitle:
            '${rental.storeName} · ${rentalStatusLabel(rental.rentalStatus)}',
      ),
      amountLabel: amountLabel,
      onPay: _pay,
      isBusy: _flow.isBusy,
      isConfigured: _flow.isConfigured,
      rows: [
        LabeledValue(label: 'Iznos', value: amountLabel),
        LabeledValue(label: 'Obračun', value: rental.chargeLabel),
      ],
    );
  }
}
