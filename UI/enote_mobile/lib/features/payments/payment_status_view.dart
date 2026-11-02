import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';
import '../../widgets/submit_button.dart';

enum PaymentFlowState { processing, succeeded, failed, pending }

class PaymentStatusView extends StatelessWidget {
  final PaymentFlowState state;

  final String? detail;

  final String? retryLabel;
  final VoidCallback? onRetry;
  final bool isBusy;

  final String cancelLabel;
  final VoidCallback? onCancel;

  final String? successNote;

  const PaymentStatusView({
    super.key,
    required this.state,
    this.detail,
    this.retryLabel,
    this.onRetry,
    this.isBusy = false,
    required this.cancelLabel,
    required this.onCancel,
    this.successNote,
  });

  @override
  Widget build(BuildContext context) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(AppSpacing.xl),
      children: [
        const SizedBox(height: AppSpacing.xxl),
        _icon(),
        const SizedBox(height: AppSpacing.lg),
        Text(
          _title,
          style: Theme.of(context).textTheme.headlineSmall,
          textAlign: TextAlign.center,
        ),
        if (detail != null) ...[
          const SizedBox(height: AppSpacing.sm),
          Text(detail!, textAlign: TextAlign.center),
        ],
        if (_note != null) ...[
          const SizedBox(height: AppSpacing.sm),
          Text(
            _note!,
            style: Theme.of(context).textTheme.bodyMedium
                ?.copyWith(color: AppTheme.textSecondary),
            textAlign: TextAlign.center,
          ),
        ],
        const SizedBox(height: AppSpacing.xl),
        if (retryLabel != null)
          SubmitButton(label: retryLabel!, onPressed: onRetry, isBusy: isBusy),
        if (retryLabel != null) const SizedBox(height: AppSpacing.sm),
        OutlinedButton(
          onPressed: isBusy ? null : onCancel,
          child: Text(cancelLabel),
        ),
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
        size: AppSize.heroIcon,
        color: AppTheme.success,
      ),
      PaymentFlowState.failed => const Icon(
        Icons.error_outline,
        size: AppSize.heroIcon,
        color: AppTheme.error,
      ),
      PaymentFlowState.pending => const Icon(
        Icons.hourglass_empty_outlined,
        size: AppSize.heroIcon,
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
    PaymentFlowState.processing =>
      'Ne zatvarajte ekran, provjeravamo plaćanje.',
    PaymentFlowState.succeeded => successNote,
    PaymentFlowState.pending => 'Provjerite ponovo za nekoliko sekundi.',
    _ => null,
  };
}
