import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

class AppSheetBody extends StatelessWidget {
  final String title;
  final String? subtitle;
  final List<Widget> children;

  const AppSheetBody({
    super.key,
    required this.title,
    this.subtitle,
    required this.children,
  });

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      child: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.lg,
            0,
            AppSpacing.lg,
            AppSpacing.lg,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(title, style: textTheme.titleLarge),
              if (subtitle != null) ...[
                const SizedBox(height: AppSpacing.xxs),
                Text(
                  subtitle!,
                  style: textTheme.bodyMedium?.copyWith(
                    color: AppTheme.textSecondary,
                  ),
                ),
              ],
              const SizedBox(height: AppSpacing.lg),
              ...children,
            ],
          ),
        ),
      ),
    );
  }
}
