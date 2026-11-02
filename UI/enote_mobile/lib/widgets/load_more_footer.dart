import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

class LoadMoreFooter extends StatelessWidget {
  final bool hasMore;
  final bool isLoading;
  final Object? error;
  final VoidCallback onLoadMore;

  const LoadMoreFooter({
    super.key,
    required this.hasMore,
    required this.isLoading,
    required this.error,
    required this.onLoadMore,
  });

  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return const Padding(
        padding: EdgeInsets.all(AppSpacing.lg),
        child: Center(
          child: SizedBox(
            width: AppSize.spinnerMd,
            height: AppSize.spinnerMd,
            child: CircularProgressIndicator(
              strokeWidth: AppSize.spinnerStroke,
            ),
          ),
        ),
      );
    }
    if (error != null) {
      return Padding(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.lg,
          vertical: AppSpacing.sm,
        ),
        child: Row(
          children: [
            Expanded(
              child: Text(
                'Učitavanje nije uspjelo.',
                style: Theme.of(context).textTheme.bodyMedium
                    ?.copyWith(color: AppTheme.textSecondary),
              ),
            ),
            TextButton(
              onPressed: onLoadMore,
              child: const Text('Pokušaj ponovo'),
            ),
          ],
        ),
      );
    }
    if (hasMore) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (context.mounted) onLoadMore();
      });
    }
    return const SizedBox.shrink();
  }
}
