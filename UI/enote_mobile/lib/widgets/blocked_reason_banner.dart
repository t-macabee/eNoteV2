import 'package:flutter/material.dart';

class BlockedReasonBanner extends StatelessWidget {
  final IconData icon;
  final String reason;
  final String? actionLabel;
  final VoidCallback? onAction;

  const BlockedReasonBanner({
    super.key,
    required this.icon,
    required this.reason,
    this.actionLabel,
    this.onAction,
  });

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            Icon(icon, color: colorScheme.error),
            const SizedBox(width: 12),
            Expanded(child: Text(reason)),
            if (actionLabel != null) ...[
              const SizedBox(width: 12),
              TextButton(onPressed: onAction, child: Text(actionLabel!)),
            ],
          ],
        ),
      ),
    );
  }
}
