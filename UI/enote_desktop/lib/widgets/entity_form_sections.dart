import 'package:enote_core/enote_core.dart';
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
              width: AppSize.spinnerSm,
              height: AppSize.spinnerSm,
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
              width: AppSize.spinnerSm,
              height: AppSize.spinnerSm,
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

class FormSectionScope extends InheritedWidget {
  final bool flat;

  const FormSectionScope({super.key, required this.flat, required super.child});

  static bool flatOf(BuildContext context) {
    final scope = context
        .dependOnInheritedWidgetOfExactType<FormSectionScope>();
    return scope?.flat ?? false;
  }

  @override
  bool updateShouldNotify(FormSectionScope oldWidget) => oldWidget.flat != flat;
}

class FormSection extends StatelessWidget {
  static const double _accentWidth = 3;

  final String title;
  final String? description;
  final List<Widget> children;

  const FormSection({
    super.key,
    required this.title,
    this.description,
    required this.children,
  });

  @override
  Widget build(BuildContext context) {
    final flat = FormSectionScope.flatOf(context);

    final titleBlock = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      spacing: AppSpacing.xs,
      children: [
        IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Container(
                key: const ValueKey('form_section_accent'),
                width: _accentWidth,
                color: AppTheme.primary,
              ),
              const SizedBox(width: AppSpacing.sm),
              Flexible(
                child: Text(
                  title,
                  style: Theme.of(context).textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.w600),
                ),
              ),
            ],
          ),
        ),
        if (description != null)
          Padding(
            padding: const EdgeInsets.only(left: _accentWidth + AppSpacing.sm),
            child: Text(
              description!,
              style: Theme.of(context).textTheme.labelMedium
                  ?.copyWith(color: AppTheme.textSecondary),
            ),
          ),
      ],
    );

    final body = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        titleBlock,
        const SizedBox(height: AppSpacing.lg),
        for (var i = 0; i < children.length; i++) ...[
          if (i > 0) const SizedBox(height: AppSpacing.lg),
          children[i],
        ],
      ],
    );

    if (flat) {
      return body;
    }

    return Container(
      decoration: BoxDecoration(
        color: AppTheme.surfaceContainer,
        border: Border.all(color: AppTheme.outline),
        borderRadius: BorderRadius.circular(AppRadius.lg),
      ),
      padding: const EdgeInsets.all(AppSpacing.xl),
      child: body,
    );
  }
}

class FormFieldRow extends StatelessWidget {
  static const double _breakpoint = 560;

  final List<Widget> children;

  const FormFieldRow({super.key, required this.children});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth >= _breakpoint) {
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (var i = 0; i < children.length; i++) ...[
                if (i > 0) const SizedBox(width: AppSpacing.lg),
                Expanded(child: children[i]),
              ],
            ],
          );
        }
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: AppSpacing.lg,
          children: children,
        );
      },
    );
  }
}
