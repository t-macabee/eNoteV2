import 'package:flutter/material.dart';

class ErrorBanner extends StatelessWidget implements PreferredSizeWidget {
  final String message;
  final VoidCallback? onDismissed;
  final Color? backgroundColor;
  final TextStyle? textStyle;

  const ErrorBanner({
    super.key,
    required this.message,
    this.onDismissed,
    this.backgroundColor,
    this.textStyle,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: backgroundColor ?? Colors.red.shade100,
      child: SizedBox(
        width: double.infinity,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            children: [
              const Icon(Icons.error_outline, color: Colors.red),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  message,
                  style: textStyle ??
                      TextStyle(color: Colors.red.shade900, fontSize: 14),
                ),
              ),
              if (onDismissed != null)
                IconButton(
                  icon: const Icon(Icons.close, size: 20),
                  onPressed: onDismissed,
                ),
            ],
          ),
        ),
      ),
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
            const Icon(Icons.error_outline, color: Colors.white),
            const SizedBox(width: 8),
            Expanded(child: Text(message)),
          ],
        ),
        backgroundColor: Colors.red.shade700,
        duration: duration ??
            (autoDismiss ? const Duration(seconds: 4) : const Duration(days: 365)),
        action: autoDismiss
            ? null
            : SnackBarAction(
                label: 'Zatvoriti',
                textColor: Colors.white,
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

  @override
  Size get preferredSize => const Size.fromHeight(56);
}
