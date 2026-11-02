import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

class DashboardStatCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final int? value;
  final bool isLoading;
  final bool hasError;
  final StatusTone tone;
  final VoidCallback? onTap;

  const DashboardStatCard({
    super.key,
    required this.icon,
    required this.label,
    required this.value,
    this.isLoading = false,
    this.hasError = false,
    this.tone = StatusTone.accent,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final content = Padding(
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(AppSpacing.sm),
                decoration: BoxDecoration(
                  color: tone.background,
                  borderRadius: BorderRadius.circular(AppRadius.md),
                ),
                child: Icon(icon, size: AppSize.spinnerMd, color: tone.color),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Text(
                  label,
                  style: Theme.of(context).textTheme.bodySmall
                      ?.copyWith(color: AppTheme.textSecondary),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),
          SizedBox(
            height: AppSpacing.xxl,
            child: Align(
              alignment: Alignment.centerLeft,
              child: isLoading
                  ? const SizedBox(
                      width: AppSize.spinnerMd,
                      height: AppSize.spinnerMd,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text(
                      hasError || value == null ? '–' : '$value',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.headlineMedium
                          ?.copyWith(color: AppTheme.textPrimary),
                    ),
            ),
          ),
        ],
      ),
    );

    return SizedBox(
      width: AppSize.statCardWidth,
      child: Card(
        color: AppTheme.surfaceContainer,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.lg),
          side: const BorderSide(color: AppTheme.outline),
        ),
        clipBehavior: Clip.antiAlias,
        child: onTap == null
            ? content
            : InkWell(
                onTap: onTap,
                borderRadius: BorderRadius.circular(AppRadius.lg),
                child: content,
              ),
      ),
    );
  }
}
