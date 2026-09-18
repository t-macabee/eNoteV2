import 'package:flutter/foundation.dart';

import 'package:enote_core/enote_core.dart';

import 'payment_sheet_gateway.dart';
import 'stripe_redirect.dart';

/// The screens of the shared payment state machine (02 §10.3 A–F).
enum PaymentView { review, processing, succeeded, failed, pending }

/// State machine behind the tuition and rental payment screens.
///
/// The screen owns the provider wiring and the review body; everything from
/// the Stripe sheet onward lives here, including the [StripeRedirect] slot
/// (claimed on construction, restored on [dispose]). Provider calls arrive as
/// callbacks so the flow can be unit-tested without widgets.
class PaymentFlowController extends ChangeNotifier {
  /// Shown when a Canceled sheet turns out to be a decline (server row is
  /// Failed).
  static const declinedMessage = 'Kartica je odbijena.';
  static const _cancelledMessage = 'Plaćanje je otkazano.';
  static const _unavailableMessage =
      'Plaćanje trenutno nije dostupno. Pokušajte ponovo kasnije.';

  final PaymentSheetGateway gateway;
  final bool isConfigured;
  final Future<String> Function() createIntent;
  final Future<PaymentRecord?> Function() status;

  /// Resolves to the latest row; anything but `succeeded` lands on pending.
  final Future<PaymentRecord?> Function() pollUntilSucceeded;

  PaymentFlowController({
    required this.gateway,
    required this.createIntent,
    required this.status,
    required this.pollUntilSucceeded,
    this.isConfigured = true,
  }) : _previousRedirectHandler = StripeRedirect.handler {
    StripeRedirect.handler = recheck;
  }

  PaymentView view = PaymentView.review;
  String? failureMessage;
  bool isBusy = false;
  bool isPolling = false;
  PaymentRecord? payment;

  final VoidCallback? _previousRedirectHandler;
  bool _disposed = false;

  @override
  void dispose() {
    if (StripeRedirect.handler == recheck) {
      StripeRedirect.handler = _previousRedirectHandler;
    }
    _disposed = true;
    super.dispose();
  }

  void _notify() {
    if (!_disposed) notifyListeners();
  }

  /// One payment attempt: create-intent → init → present → poll.
  Future<void> attempt() async {
    if (isBusy) return;
    isBusy = true;
    _notify();
    try {
      if (!isConfigured) {
        view = PaymentView.failed;
        failureMessage = _unavailableMessage;
        return;
      }
      final clientSecret = await createIntent();
      await gateway.init(clientSecret);
      await gateway.present();
    } on PaymentSheetCancelled {
      // flutter_stripe 14.0.0 reports a decline-then-dismiss as
      // FailureCode.Canceled, identical to a walk-away: one server read
      // disambiguates (the payment_failed webhook marks the row Failed).
      var message = _cancelledMessage;
      try {
        final current = await status();
        if (current?.status == PaymentStatus.failed) {
          message = declinedMessage;
        }
      } catch (_) {
        // The read itself failed: keep the cancel copy.
      }
      view = PaymentView.failed;
      failureMessage = message;
      return;
    } on PaymentSheetFailure catch (e) {
      view = PaymentView.failed;
      failureMessage = e.message;
      return;
    } catch (e) {
      view = PaymentView.failed;
      failureMessage = userMessage(e);
      return;
    } finally {
      isBusy = false;
      _notify();
    }
    view = PaymentView.processing;
    _notify();
    await poll();
  }

  /// Deep-link re-entry (`enote://stripe-redirect`): poll again only while
  /// waiting on the server.
  void recheck() {
    if (_disposed || view != PaymentView.pending || isPolling) return;
    poll();
  }

  /// One poll cycle (state F `Provjeri ponovo`, deep-link re-entry).
  Future<void> poll() async {
    if (isPolling) return;
    isPolling = true;
    _notify();
    try {
      final result = await pollUntilSucceeded();
      isPolling = false;
      if (result?.status == PaymentStatus.succeeded) {
        payment = result;
        view = PaymentView.succeeded;
      } else {
        view = PaymentView.pending;
      }
      _notify();
    } catch (e) {
      isPolling = false;
      view = PaymentView.failed;
      failureMessage = userMessage(e);
      _notify();
    }
  }
}
