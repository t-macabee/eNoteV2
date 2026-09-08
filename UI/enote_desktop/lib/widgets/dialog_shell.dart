import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// Supported maximum widths for [DialogShell].
enum DialogShellWidth {
  sm(480),
  md(620),
  lg(800),
  xl(960);

  final double value;
  const DialogShellWidth(this.value);

  double get maxWidth => value;
}

/// Compact single-row dialog header shared by workspace and sibling dialogs.
///
/// Renders `leading`, `label`, `action` and a close button in one row with
/// the workspace padding (`EdgeInsets.fromLTRB(8, 4, 8, 4)`). When there is
/// no [leading], nothing is rendered in its slot and the label is padded
/// with `EdgeInsets.only(left: 16)` so the text starts at a normal dialog
/// margin instead of leaving dead space.
class DialogShellHeader extends StatelessWidget {
  final Widget? leading;
  final Widget label;
  final Widget? action;
  final VoidCallback onClose;

  const DialogShellHeader({
    super.key,
    this.leading,
    required this.label,
    this.action,
    required this.onClose,
  });

  @override
  Widget build(BuildContext context) {
    final leading = this.leading;
    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 4, 8, 4),
      child: Row(
        children: [
          ?leading,
          Expanded(
            child: Padding(
              padding: leading == null
                  ? const EdgeInsets.only(left: 16)
                  : EdgeInsets.zero,
              child: label,
            ),
          ),
          if (action != null) ...[
            action!,
            const SizedBox(width: 8),
          ],
          IconButton(
            icon: const Icon(Icons.close),
            tooltip: 'Zatvori',
            onPressed: onClose,
          ),
        ],
      ),
    );
  }
}

/// A reusable dialog frame matching the application theme.
class DialogShell extends StatelessWidget {
  final String? title;
  final Widget body;
  final DialogShellWidth width;
  final Widget? headerAction;
  final Widget? leading;
  final VoidCallback? onClose;
  final Widget? header;

  const DialogShell({
    super.key,
    this.title,
    required this.body,
    this.width = DialogShellWidth.md,
    this.headerAction,
    this.leading,
    this.onClose,
    this.header,
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
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (header != null)
              header!
            else
              Padding(
                padding: const EdgeInsets.fromLTRB(24, 16, 16, 16),
                child: Row(
                  children: [
                    if (leading != null) ...[
                      leading!,
                      const SizedBox(width: 8),
                    ],
                    Expanded(
                      child: Text(
                        title ?? '',
                        style: Theme.of(context).textTheme.titleLarge,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
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
            Flexible(child: body),
          ],
        ),
      ),
    );
  }
}
