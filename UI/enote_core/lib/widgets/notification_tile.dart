import 'package:flutter/material.dart';

import '../formatting/formatters.dart';
import '../models/communication/communication_models.dart';
import '../theme/app_theme.dart';
import '../theme/app_tokens.dart';

class NotificationTile extends StatelessWidget {
  static const double dotSize = 8;

  final NotificationDto notification;
  final VoidCallback? onTap;

  const NotificationTile({
    super.key,
    required this.notification,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final unread = !notification.isRead;
    final titleColor = unread ? AppTheme.textPrimary : AppTheme.textSecondary;

    return ListTile(
      onTap: onTap,
      contentPadding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.lg,
        vertical: AppSpacing.sm,
      ),
      leading: Container(
        width: dotSize,
        height: dotSize,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: unread ? AppTheme.primary : null,
          border: unread
              ? null
              : Border.all(color: AppTheme.textSecondary, width: 1.5),
        ),
      ),
      title: Row(
        children: [
          Expanded(
            child: Text(
              notification.title,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: textTheme.bodyLarge?.copyWith(
                fontWeight: unread ? FontWeight.w600 : FontWeight.w400,
                color: titleColor,
              ),
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          Text(
            formatDayTime(notification.createdAt),
            style: textTheme.bodySmall?.copyWith(
              color: unread ? AppTheme.textSecondary : AppTheme.textTertiary,
            ),
          ),
        ],
      ),
      subtitle: Text(
        notification.body,
        maxLines: 2,
        overflow: TextOverflow.ellipsis,
        style: textTheme.bodyMedium?.copyWith(
          color: unread ? AppTheme.textPrimary : AppTheme.textSecondary,
        ),
      ),
    );
  }
}
