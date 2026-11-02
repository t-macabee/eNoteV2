import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

class AsyncStateView extends StatelessWidget {
  final bool isLoading;
  final Object? error;
  final bool isEmpty;
  final String emptyMessage;
  final IconData emptyIcon;
  final Widget? emptyAction;
  final VoidCallback onRetry;
  final Widget child;

  const AsyncStateView({
    super.key,
    this.isLoading = false,
    this.error,
    this.isEmpty = false,
    this.emptyMessage = 'Nema podataka.',
    this.emptyIcon = Icons.inbox_outlined,
    this.emptyAction,
    required this.onRetry,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (error != null) {
      return _StateMessage(
        icon: Icons.warning_amber_outlined,
        message: userMessage(error!),
        action: OutlinedButton(
          onPressed: onRetry,
          child: const Text('Pokušaj ponovo'),
        ),
      );
    }
    if (isEmpty) {
      return _StateMessage(
        icon: emptyIcon,
        message: emptyMessage,
        action: emptyAction,
      );
    }
    return child;
  }
}

class _StateMessage extends StatelessWidget {
  final IconData icon;
  final String message;
  final Widget? action;

  const _StateMessage({required this.icon, required this.message, this.action});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: AppSize.stateIcon, color: AppTheme.textTertiary),
            const SizedBox(height: AppSpacing.md),
            Text(
              message,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyMedium
                  ?.copyWith(color: AppTheme.textSecondary),
            ),
            if (action != null) ...[
              const SizedBox(height: AppSpacing.lg),
              action!,
            ],
          ],
        ),
      ),
    );
  }
}
