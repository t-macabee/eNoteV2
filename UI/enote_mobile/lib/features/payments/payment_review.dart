import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';
import '../../widgets/detail_actions.dart';
import '../../widgets/submit_button.dart';

class PaymentReview extends StatelessWidget {
  final Widget header;
  final String amountLabel;
  final VoidCallback onPay;
  final bool isBusy;
  final bool isConfigured;
  final List<Widget> rows;

  const PaymentReview({
    super.key,
    required this.header,
    required this.amountLabel,
    required this.onPay,
    required this.isBusy,
    required this.isConfigured,
    required this.rows,
  });

  @override
  Widget build(BuildContext context) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.only(bottom: AppSpacing.lg),
      children: [
        header,
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: rows,
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.lg,
            AppSpacing.lg,
            AppSpacing.lg,
            0,
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(Icons.info_outline, color: AppTheme.textSecondary),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  'Plaćanje se obavlja putem Stripe-a. '
                  'Podaci o kartici se ne pohranjuju u aplikaciji.',
                  style: Theme.of(context).textTheme.bodyMedium
                      ?.copyWith(color: AppTheme.textSecondary),
                ),
              ),
            ],
          ),
        ),
        DetailActions(
          banner: isConfigured
              ? null
              : const ErrorBanner(
                  message: 'Plaćanje nije podešeno u ovoj verziji aplikacije.',
                ),
          actions: [
            SubmitButton(
              label: 'Plati $amountLabel',
              onPressed: isConfigured ? onPay : null,
              isBusy: isBusy,
            ),
          ],
        ),
      ],
    );
  }
}
