import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';

/// The review state (A) shared by the tuition and rental payment screens:
/// the calling screen's summary rows, the Stripe note and the pay button.
class PaymentReview extends StatelessWidget {
  final double? amount;
  final VoidCallback onPay;
  final bool isBusy;
  final List<Widget> children;

  const PaymentReview({
    super.key,
    required this.amount,
    required this.onPay,
    required this.isBusy,
    required this.children,
  });

  @override
  Widget build(BuildContext context) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      children: [
        ...children,
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
          onPressed: isBusy ? null : onPay,
          child: isBusy
              ? const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : Text('Plati ${formatKM(amount)}'),
        ),
      ],
    );
  }
}
