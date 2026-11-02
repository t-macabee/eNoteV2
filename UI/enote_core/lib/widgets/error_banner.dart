import 'package:flutter/material.dart';

import '../theme/app_theme.dart';
import '../theme/app_tokens.dart';
import 'status_banner.dart';

class ErrorBanner extends StatelessWidget {
  final String message;

  const ErrorBanner({super.key, required this.message});

  @override
  Widget build(BuildContext context) {
    return StatusBanner(
      icon: Icons.error_outline,
      message: message,
      tone: StatusTone.danger,
    );
  }

  static void show(
    BuildContext context, {
    required String message,
    Key? key,
    VoidCallback? onDismissed,
    bool autoDismiss = true,
    Duration? duration,
  }) {
    final messenger = ScaffoldMessenger.of(context);
    messenger.showSnackBar(
      SnackBar(
        key: key,
        content: Row(
          children: [
            const Icon(Icons.error_outline, color: AppTheme.onError),
            const SizedBox(width: AppSpacing.sm),
            Expanded(child: Text(message)),
          ],
        ),
        backgroundColor: AppTheme.error,
        duration:
            duration ??
            (autoDismiss
                ? const Duration(seconds: 4)
                : const Duration(days: 365)),
        action: autoDismiss
            ? null
            : SnackBarAction(
                label: 'Zatvori',
                textColor: AppTheme.onError,
                onPressed: () {
                  if (onDismissed != null) {
                    onDismissed();
                  } else {
                    messenger.hideCurrentSnackBar();
                  }
                },
              ),
      ),
    );
  }
}
