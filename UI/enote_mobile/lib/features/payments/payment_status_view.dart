import 'package:flutter/material.dart';

import '../../theme/app_theme.dart';

/// The poll-driven payment states of S12 (02 §10.3 C–F).
enum PaymentFlowState { processing, succeeded, failed, pending }

/// States C–F of the payment flow: the review screen (state A) is built by
/// `PaymentScreen` itself.
class PaymentStatusView extends StatelessWidget {
  final PaymentFlowState state;

  /// D subtitle (`{amount} · {dateTime}`) or E detail message.
  final String? detail;

  /// Retry button label (`Pokušaj ponovo` on E, `Provjeri ponovo` on F).
  final String? retryLabel;
  final VoidCallback? onRetry;
  final bool isBusy;

  /// Cancel/back button label (`Odustani` on E, `Nazad` on F,
  /// `Nazad na iznajmljivanje` on D).
  final String cancelLabel;
  final VoidCallback? onCancel;

  const PaymentStatusView({
    super.key,
    required this.state,
    this.detail,
    this.retryLabel,
    this.onRetry,
    this.isBusy = false,
    required this.cancelLabel,
    required this.onCancel,
  });

  @override
  Widget build(BuildContext context) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(24),
      children: [
        const SizedBox(height: 48),
        _icon(),
        const SizedBox(height: 16),
        Text(
          _title,
          style: Theme.of(context).textTheme.headlineSmall,
          textAlign: TextAlign.center,
        ),
        if (detail != null) ...[
          const SizedBox(height: 8),
          Text(detail!, textAlign: TextAlign.center),
        ],
        if (_note != null) ...[
          const SizedBox(height: 8),
          Text(
            _note!,
            style: const TextStyle(color: AppTheme.textSecondary),
            textAlign: TextAlign.center,
          ),
        ],
        const SizedBox(height: 24),
        if (retryLabel != null)
          FilledButton(
            onPressed: isBusy ? null : onRetry,
            child: isBusy
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Text(retryLabel!),
          ),
        if (retryLabel != null) const SizedBox(height: 8),
        OutlinedButton(onPressed: isBusy ? null : onCancel, child: Text(cancelLabel)),
      ],
    );
  }

  Widget _icon() {
    return switch (state) {
      PaymentFlowState.processing => const Center(
        child: CircularProgressIndicator(),
      ),
      PaymentFlowState.succeeded => const Icon(
        Icons.check_circle_outline,
        size: 64,
        color: AppTheme.success,
      ),
      PaymentFlowState.failed => const Icon(
        Icons.error_outline,
        size: 64,
        color: AppTheme.error,
      ),
      PaymentFlowState.pending => const Icon(
        Icons.hourglass_empty_outlined,
        size: 64,
        color: AppTheme.warning,
      ),
    };
  }

  String get _title => switch (state) {
    PaymentFlowState.processing => 'Provjeravamo status plaćanja…',
    PaymentFlowState.succeeded => 'Plaćanje uspješno',
    PaymentFlowState.failed => 'Plaćanje nije dovršeno',
    PaymentFlowState.pending => 'Plaćanje se obrađuje',
  };

  String? get _note => switch (state) {
    PaymentFlowState.succeeded => 'Iznajmljivanje je izmireno.',
    PaymentFlowState.pending =>
      'Provjerite ponovo za nekoliko sekundi.',
    _ => null,
  };
}
