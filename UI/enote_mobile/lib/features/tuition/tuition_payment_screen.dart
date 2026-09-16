import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../config.dart';
import '../../theme/app_theme.dart';
import '../../widgets/labeled_value.dart';
import '../payments/payment_sheet_gateway.dart';
import '../payments/payment_status_view.dart';
import '../payments/stripe_redirect.dart';
import 'tuition_payment_provider.dart';

enum _View { review, processing, succeeded, failed, pending }

/// The tuition payment flow for one enrollment: review (A), Stripe sheet (B),
/// processing (C), succeeded (D), failed/cancelled (E), pending (F).
///
/// Mirrors the rental screen but needs no server load for state A: the course
/// name and monthly price travel with the route arguments. The screen never
/// caches a `clientSecret` and never renders `paymentIntentId`.
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
  /// Shown when a Canceled sheet turns out to be a decline (server row is
  /// Failed).
  static const _declinedMessage = 'Kartica je odbijena.';

  CoursePaymentDto? _payment;
  bool _isBusy = false;
  bool _isPolling = false;
  _View _view = _View.review;
  String? _failureMessage;
  VoidCallback? _previousRedirectHandler;

  @override
  void initState() {
    super.initState();
    _previousRedirectHandler = StripeRedirect.handler;
    StripeRedirect.handler = recheck;
  }

  @override
  void dispose() {
    if (StripeRedirect.handler == recheck) {
      StripeRedirect.handler = _previousRedirectHandler;
    }
    super.dispose();
  }

  /// One more poll cycle (F `Provjeri ponovo`, deep-link re-entry).
  void recheck() {
    if (!mounted || _view != _View.pending || _isPolling) return;
    _poll();
  }

  Future<void> _pay() async {
    if (_isBusy) return;
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrda plaćanja',
      message:
          'Platiti ${formatKM(widget.price)} za kurs "${widget.courseName}" '
          'putem Stripe-a?',
    );
    if (confirmed != true || !mounted) return;
    await _attempt();
  }

  /// One payment attempt: create-intent → init → present → poll.
  Future<void> _attempt() async {
    final payments = context.read<TuitionPaymentProvider>();
    final enrollmentId = widget.enrollmentId;
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
      final intent = await payments.createIntent(enrollmentId);
      await widget.gateway.init(intent.clientSecret);
      await widget.gateway.present();
    } on PaymentSheetCancelled {
      var failureMessage = 'Plaćanje je otkazano.';
      try {
        final current = await payments.status(enrollmentId);
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
    if (_isPolling) return;
    final payments = context.read<TuitionPaymentProvider>();
    setState(() => _isPolling = true);
    try {
      final result = await payments.pollUntilSucceeded(widget.enrollmentId);
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

  void _backToCourse() {
    if (mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: _view != _View.processing,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Plaćanje školarine'),
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
        body: switch (_view) {
          _View.review => _review(),
          _View.processing => const PaymentStatusView(
            state: PaymentFlowState.processing,
            cancelLabel: 'Nazad',
            onCancel: null,
          ),
          _View.succeeded => PaymentStatusView(
            state: PaymentFlowState.succeeded,
            detail: _successDetail(),
            successNote: 'Školarina je plaćena.',
            cancelLabel: 'Nazad na kurs',
            onCancel: _backToCourse,
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
        },
      ),
    );
  }

  String? _successDetail() {
    final payment = _payment;
    if (payment == null) return null;
    return formatPaymentDetail(payment.amountCents, payment.paidAt, currency: payment.currency);
  }

  Widget _review() {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      children: [
        Text(widget.courseName, style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 16),
        LabeledValue(label: 'Iznos', value: formatKM(widget.price)),
        const LabeledValue(label: 'Trajanje pristupa', value: '30 dana'),
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
              : Text('Plati ${formatKM(widget.price)}'),
        ),
      ],
    );
  }
}
