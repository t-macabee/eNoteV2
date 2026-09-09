import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class AsyncStateView extends StatelessWidget {
  final bool isLoading;
  final Object? error;
  final bool isEmpty;
  final String emptyMessage;
  final Widget? emptyAction;
  final VoidCallback onRetry;
  final Widget child;

  const AsyncStateView({
    super.key,
    this.isLoading = false,
    this.error,
    this.isEmpty = false,
    this.emptyMessage = 'Nema rezultata za pretragu.',
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
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.warning_amber_outlined, size: 48),
              const SizedBox(height: 12),
              Text(userMessage(error!), textAlign: TextAlign.center),
              const SizedBox(height: 16),
              OutlinedButton(
                onPressed: onRetry,
                child: const Text('Pokušaj ponovo'),
              ),
            ],
          ),
        ),
      );
    }
    if (isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.inbox_outlined, size: 48),
              const SizedBox(height: 12),
              Text(emptyMessage, textAlign: TextAlign.center),
              if (emptyAction != null) ...[
                const SizedBox(height: 16),
                emptyAction!,
              ],
            ],
          ),
        ),
      );
    }
    return child;
  }
}
