import 'package:flutter/foundation.dart';

import 'package:enote_core/enote_core.dart';

import 'payment_sheet_gateway.dart';
import 'stripe_redirect.dart';

enum PaymentView { review, processing, succeeded, failed, pending }

class PaymentFlowController extends ChangeNotifier {
  static const declinedMessage = 'Kartica je odbijena.';
  static const _cancelledMessage = 'Plaćanje je otkazano.';
  static const _unavailableMessage =
      'Plaćanje trenutno nije dostupno. Pokušajte ponovo kasnije.';

  final PaymentSheetGateway gateway;
  final bool isConfigured;
  final Future<PaymentIntentDraft> Function() createIntent;
  final Future<PaymentRecord?> Function() status;

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
  PaymentIntentDraft? prepared;

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

  Future<PaymentIntentDraft?> prepare() async {
    if (isBusy) return null;
    prepared = null;
    if (!isConfigured) {
      view = PaymentView.failed;
      failureMessage = _unavailableMessage;
      _notify();
      return null;
    }
    isBusy = true;
    _notify();
    try {
      final intent = await createIntent();
      prepared = intent;
      return intent;
    } catch (e) {
      view = PaymentView.failed;
      failureMessage = userMessage(e);
      return null;
    } finally {
      isBusy = false;
      _notify();
    }
  }

  Future<void> confirmAndPresent() async {
    final intent = prepared;
    if (intent == null || isBusy) return;
    isBusy = true;
    _notify();
    try {
      await gateway.init(intent.clientSecret);
      await gateway.present();
    } on PaymentSheetCancelled {
      var message = _cancelledMessage;
      try {
        final current = await status();
        if (current?.status == PaymentStatus.failed) {
          message = declinedMessage;
        }
      } catch (_) {
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
    failureMessage = null;
    _notify();
    await poll();
  }

  void retry() {
    if (isBusy) return;
    prepared = null;
    failureMessage = null;
    view = PaymentView.review;
    _notify();
  }

  void recheck() {
    if (_disposed || view != PaymentView.pending || isPolling) return;
    poll();
  }

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
    } catch (_) {
      isPolling = false;
      view = PaymentView.pending;
      _notify();
    }
  }
}
