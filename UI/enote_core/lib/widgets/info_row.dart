import 'package:flutter/material.dart';

import '../theme/app_tokens.dart';

class InfoRow extends StatelessWidget {
  final IconData? icon;
  final String label;
  final String value;
  final double labelWidth;

  const InfoRow({
    super.key,
    this.icon,
    required this.label,
    required this.value,
    this.labelWidth = 140,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 20, color: colorScheme.onSurfaceVariant),
            const SizedBox(width: AppSpacing.md),
          ],
          SizedBox(
            width: labelWidth,
            child: Text(
              label,
              style: theme.textTheme.bodySmall?.copyWith(
                color: colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: colorScheme.onSurface,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
