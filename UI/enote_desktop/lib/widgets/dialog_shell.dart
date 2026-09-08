import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// Supported maximum widths for [DialogShell].
enum DialogShellWidth {
  md(640),
  lg(900),
  xl(1100);

  final double value;
  const DialogShellWidth(this.value);

  double get maxWidth => value;
}

/// A reusable dialog frame matching the application theme.
class DialogShell extends StatelessWidget {
  final String title;
  final Widget body;
  final DialogShellWidth width;
  final Widget? headerAction;
  final Widget? leading;
  final VoidCallback? onClose;

  const DialogShell({
    super.key,
    required this.title,
    required this.body,
    this.width = DialogShellWidth.md,
    this.headerAction,
    this.leading,
    this.onClose,
  });

  @override
  Widget build(BuildContext context) {
    final maxHeight = MediaQuery.sizeOf(context).height * 0.85;

    return Dialog(
      backgroundColor: AppTheme.surfaceContainer,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: const BorderSide(color: AppTheme.outline),
      ),
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: width.value,
          maxHeight: maxHeight,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 16, 16, 16),
              child: Row(
                children: [
                  if (leading != null) ...[
                    leading!,
                    const SizedBox(width: 8),
                  ],
                  Flexible(
                    child: Text(
                      title,
                      style: Theme.of(context).textTheme.titleLarge,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  const Spacer(),
                  if (headerAction != null) ...[
                    headerAction!,
                    const SizedBox(width: 8),
                  ],
                  IconButton(
                    icon: const Icon(Icons.close),
                    tooltip: 'Zatvori',
                    onPressed: onClose ?? () => Navigator.of(context).pop(),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            Expanded(child: body),
          ],
        ),
      ),
    );
  }
}
