import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../config.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/labeled_value.dart';
import '../../widgets/status_chip.dart';
import '../rentals/rental_provider.dart';
import 'payment_provider.dart';
import 'payment_sheet_gateway.dart';
import 'payment_status_view.dart';

enum _View { review, processing, succeeded, failed, pending }

/// S12 — the payment flow (02 §4.3, §10): review (A), Stripe sheet (B),
/// processing (C), succeeded (D), failed/cancelled (E), pending (F).
///
/// The screen never caches a `clientSecret` (02 §10.4) and never renders
/// `paymentIntentId` (02 §10.5).
class PaymentScreen extends StatefulWidget {
  final int rentalId;
  final PaymentSheetGateway gateway;
  final String stripePublishableKey;

  /// Called by `DeepLinkHandler` on `enote://stripe-redirect` while this
  /// screen is mounted (01 §5.4 step 5): re-runs one poll cycle.
  static VoidCallback? stripeRedirectHandler;

  PaymentScreen({
    super.key,
    required this.rentalId,
    PaymentSheetGateway? gateway,
    String? stripePublishableKey,
  }) : gateway = gateway ?? StripePaymentSheetGateway(),
       stripePublishableKey =
           stripePublishableKey ?? kStripePublishableKey;

  @override
  State<PaymentScreen> createState() => _PaymentScreenState();
}

class _PaymentScreenState extends State<PaymentScreen> {
  /// Shown when a Canceled sheet turns out to be a decline (server row is
  /// Failed). Provisional wording: 02 §11 defines no decline string.
  static const _declinedMessage = 'Kartica je odbijena.';

  InstrumentRentalDto? _rental;
  RentalPaymentDto? _payment;
  Object? _error;
  bool _isLoading = true;
  bool _isBusy = false;
  bool _isPolling = false;
  _View _view = _View.review;
  String? _failureMessage;

  @override
  void initState() {
    super.initState();
    PaymentScreen.stripeRedirectHandler = recheck;
    _load();
  }

  @override
  void dispose() {
    if (PaymentScreen.stripeRedirectHandler == recheck) {
      PaymentScreen.stripeRedirectHandler = null;
    }
    super.dispose();
  }

  /// One more poll cycle (F `Provjeri ponovo`, deep-link re-entry).
  void recheck() {
    if (!mounted || _view != _View.pending || _isPolling) return;
    _poll();
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
    if (rental == null || _isBusy) return;
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrda plaćanja',
      message:
          'Platiti ${formatKM(rental.totalFee)} za ${rental.instrumentModel} putem Stripe-a?',
    );
    if (confirmed != true || !mounted) return;
    await _attempt();
  }

  /// One payment attempt: create-intent → init → present → poll (02 §10.2).
  Future<void> _attempt() async {
    final payments = context.read<PaymentProvider>();
    final rentalId = widget.rentalId;
    setState(() => _isBusy = true);
    try {
      if (widget.stripePublishableKey.isEmpty) {
        if (mounted) {
          setState(() {
            _view = _View.failed;
            _failureMessage =
                'Plaćanje trenutno nije dostupno. Pokušajte ponovo kasnije.';
          });
        }
        return;
      }
      final intent = await payments.createIntent(rentalId);
      await widget.gateway.init(intent.clientSecret);
      await widget.gateway.present();
    } on PaymentSheetCancelled {
      // flutter_stripe 14.0.0 reports a decline-then-dismiss as
      // FailureCode.Canceled, identical to a walk-away: one server read
      // disambiguates (the payment_failed webhook marks the row Failed).
      var failureMessage = 'Plaćanje je otkazano.';
      try {
        final current = await payments.status(rentalId);
        if (current?.status == PaymentStatus.failed) {
          failureMessage = _declinedMessage;
        }
      } catch (_) {
        // The read itself failed: keep the cancel copy.
      }
      if (mounted) {
        setState(() {
          _view = _View.failed;
          _failureMessage = failureMessage;
        });
      }
      return;
    } on PaymentSheetFailure catch (e) {
      if (mounted) {
        setState(() {
          _view = _View.failed;
          _failureMessage = e.message;
        });
      }
      return;
    } catch (e) {
      if (mounted) {
        setState(() {
          _view = _View.failed;
          _failureMessage = userMessage(e);
        });
      }
      return;
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
    if (!mounted) return;
    setState(() => _view = _View.processing);
    await _poll();
  }

  Future<void> _poll() async {
    final payments = context.read<PaymentProvider>();
    setState(() => _isPolling = true);
    try {
      final result = await payments.pollUntilSucceeded(widget.rentalId);
      if (!mounted) return;
      setState(() {
        _isPolling = false;
        if (result?.status == PaymentStatus.succeeded) {
          _payment = result;
          _view = _View.succeeded;
        } else {
          _view = _View.pending;
        }
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isPolling = false;
        _view = _View.failed;
        _failureMessage = userMessage(e);
      });
    }
  }

  Future<void> _backToRental() async {
    try {
      await context.read<RentalProvider>().getById(widget.rentalId);
    } catch (_) {}
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
    return PopScope(
      canPop: _view != _View.processing,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Plaćanje'),
          actions: [
            if (_view == _View.review ||
                _view == _View.failed ||
                _view == _View.pending)
              IconButton(
                icon: const Icon(Icons.close),
                onPressed: () => Navigator.of(context).maybePop(),
              ),
          ],
        ),
        body: rental == null
            ? AsyncStateView(
                isLoading: _isLoading,
                error: _error,
                onRetry: _load,
                child: const SizedBox.shrink(),
              )
            : _body(rental),
      ),
    );
  }

  Widget _body(InstrumentRentalDto rental) {
    if (rental.isPaid) {
      return _refusal('Ovo iznajmljivanje je već plaćeno.');
    }
    return switch (_view) {
      _View.review => _review(rental),
      _View.processing => const PaymentStatusView(
        state: PaymentFlowState.processing,
        cancelLabel: 'Nazad',
        onCancel: null,
      ),
      _View.succeeded => PaymentStatusView(
        state: PaymentFlowState.succeeded,
        detail: _successDetail(),
        cancelLabel: 'Nazad na iznajmljivanje',
        onCancel: _backToRental,
      ),
      _View.failed => PaymentStatusView(
        state: PaymentFlowState.failed,
        detail: _failureMessage,
        retryLabel: 'Pokušaj ponovo',
        onRetry: _isBusy ? null : _attempt,
        isBusy: _isBusy,
        cancelLabel: 'Odustani',
        onCancel: () => Navigator.of(context).maybePop(),
      ),
      _View.pending => PaymentStatusView(
        state: PaymentFlowState.pending,
        retryLabel: 'Provjeri ponovo',
        onRetry: _isPolling ? null : _poll,
        isBusy: _isPolling,
        cancelLabel: 'Nazad',
        onCancel: () => Navigator.of(context).maybePop(),
      ),
    };
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

  String? _successDetail() {
    final payment = _payment;
    if (payment == null) return null;
    final parts = <String>[formatKM(payment.amountCents / 100)];
    final paidAt = payment.paidAt;
    if (paidAt != null) parts.add(formatDateTime(paidAt));
    return parts.join(' · ');
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
          onPressed: _isBusy ? null : _pay,
          child: _isBusy
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
