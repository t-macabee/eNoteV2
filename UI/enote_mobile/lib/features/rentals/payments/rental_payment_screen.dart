import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../config.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/async_state_view.dart';
import '../../../widgets/labeled_value.dart';
import '../../../widgets/status_chip.dart';
import '../rental_provider.dart';
import '../../payments/payment_flow_body.dart';
import '../../payments/payment_flow_controller.dart';
import '../../payments/payment_sheet_gateway.dart';
import 'rental_payment_provider.dart';

/// S12 — the payment flow (02 §4.3, §10): review (A), Stripe sheet (B),
/// processing (C), succeeded (D), failed/cancelled (E), pending (F).
///
/// The screen never caches a `clientSecret` (02 §10.4) and never renders
/// `paymentIntentId` (02 §10.5).
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

  /// Read per call, not in `initState`: callers that never pay (e.g. the
  /// instrument detail deep link) need not provide it.
  RentalPaymentProvider get _payments => context.read<RentalPaymentProvider>();

  @override
  void initState() {
    super.initState();
    _flow = PaymentFlowController(
      gateway: widget.gateway,
      isConfigured: widget.stripePublishableKey.isNotEmpty,
      createIntent: () async =>
          (await _payments.createIntent(widget.rentalId)).clientSecret,
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
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrda plaćanja',
      message:
          'Platiti ${formatKM(rental.totalFee)} za ${rental.instrumentModel} putem Stripe-a?',
    );
    if (confirmed != true || !mounted) return;
    await _flow.attempt();
  }

  void _backToRental() {
    if (mounted) Navigator.of(context).pop(true);
  }

  String _charge(InstrumentRentalDto rental) {
    final parts = <String>[];
    final months = rental.monthsCharged ?? 0;
    final days = rental.daysCharged ?? 0;
    if (days > 0) parts.add('$days dana × ${formatKM(rental.dailyFee)}');
    if (months > 0) parts.add('$months mj. × ${formatKM(rental.fee)}');
    if (parts.isEmpty) return '—';
    final base = parts.join(' / ');
    return rental.isProrated ? '$base · proporcionalno' : base;
  }

  @override
  Widget build(BuildContext context) {
    final rental = _rental;
    return PaymentFlowScaffold(
      controller: _flow,
      title: 'Plaćanje',
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
      return _refusal('Ovo iznajmljivanje je već plaćeno.');
    }
    return PaymentFlowBody(
      controller: _flow,
      reviewChild: _review(rental),
      successCancelLabel: 'Nazad na iznajmljivanje',
      onSuccessBack: _backToRental,
    );
  }

  Widget _refusal(String message) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(24),
      children: [
        const SizedBox(height: 48),
        Text(message, textAlign: TextAlign.center),
        const SizedBox(height: 24),
        OutlinedButton(
          onPressed: () => Navigator.of(context).maybePop(),
          child: const Text('Nazad'),
        ),
      ],
    );
  }

  Widget _review(InstrumentRentalDto rental) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ImageThumbnail(
              imageUrl: rental.instrumentImagePath,
              apiClient: context.read<ApiClient>(),
              size: 56,
              borderRadius: 8,
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    rental.instrumentModel,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 2),
                  Text(
                    '${rental.storeName} · ${StatusChip.rental(rental.rentalStatus).label}',
                    style: const TextStyle(color: AppTheme.textSecondary),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        LabeledValue(label: 'Iznos', value: formatKM(rental.totalFee)),
        LabeledValue(label: 'Obračun', value: _charge(rental)),
        const SizedBox(height: 16),
        const Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.info_outline, color: AppTheme.textSecondary),
            SizedBox(width: 8),
            Expanded(
              child: Text(
                'Plaćanje se obavlja putem Stripe-a. '
                'Podaci o kartici se ne pohranjuju u aplikaciji.',
                style: TextStyle(color: AppTheme.textSecondary),
              ),
            ),
          ],
        ),
        const SizedBox(height: 24),
        FilledButton(
          onPressed: _flow.isBusy ? null : _pay,
          child: _flow.isBusy
              ? const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : Text('Plati ${formatKM(rental.totalFee)}'),
        ),
      ],
    );
  }
}
