import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

class DetailHeader extends StatelessWidget {
  final String title;
  final String? subtitle;
  final List<Widget> chips;
  final Widget? leading;
  final VoidCallback? onTap;

  const DetailHeader({
    super.key,
    required this.title,
    this.subtitle,
    this.chips = const [],
    this.leading,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final content = Padding(
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (leading != null) ...[
            leading!,
            const SizedBox(width: AppSpacing.lg),
          ],
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: textTheme.headlineSmall),
                if (subtitle != null) ...[
                  const SizedBox(height: AppSpacing.xxs),
                  Text(
                    subtitle!,
                    style: textTheme.bodyMedium?.copyWith(
                      color: AppTheme.textSecondary,
                    ),
                  ),
                ],
                if (chips.isNotEmpty) ...[
                  const SizedBox(height: AppSpacing.sm),
                  Wrap(
                    spacing: AppSpacing.sm,
                    runSpacing: AppSpacing.xs,
                    children: chips,
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
    if (onTap == null) return content;
    return InkWell(onTap: onTap, child: content);
  }
}
