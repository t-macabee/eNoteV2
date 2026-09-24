import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

class EntitySaveButton extends StatelessWidget {
  final bool isSaving;
  final bool isDeleting;
  final bool enabled;
  final VoidCallback onPressed;
  final String label;

  const EntitySaveButton({
    super.key,
    required this.isSaving,
    required this.isDeleting,
    required this.enabled,
    required this.onPressed,
    required this.label,
  });

  @override
  Widget build(BuildContext context) {
    return FilledButton.icon(
      onPressed: (isSaving || isDeleting || !enabled) ? null : onPressed,
      icon: isSaving
          ? const SizedBox(
              width: 16,
              height: 16,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : const Icon(Icons.save),
      label: Text(label),
    );
  }
}

class EntityDeleteButton extends StatelessWidget {
  final bool isSaving;
  final bool isDeleting;
  final VoidCallback? onPressed;
  final String label;

  const EntityDeleteButton({
    super.key,
    required this.isSaving,
    required this.isDeleting,
    this.onPressed,
    required this.label,
  });

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      onPressed: (isSaving || isDeleting) ? null : onPressed,
      style: OutlinedButton.styleFrom(
        foregroundColor: AppTheme.error,
        side: const BorderSide(color: AppTheme.error),
      ),
      icon: isDeleting
          ? const SizedBox(
              width: 16,
              height: 16,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                color: AppTheme.error,
              ),
            )
          : const Icon(Icons.delete_outline),
      label: Text(label),
    );
  }
}
