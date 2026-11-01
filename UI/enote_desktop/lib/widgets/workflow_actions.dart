import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class WorkflowAction<T> {
  final T trigger;
  final String label;
  final IconData icon;
  final StatusTone? tone;
  final bool requiresNote;
  final String? confirmMessage;
  final String? dialogTitle;

  const WorkflowAction({
    required this.trigger,
    required this.label,
    required this.icon,
    this.tone,
    this.requiresNote = false,
    this.confirmMessage,
    this.dialogTitle,
  });

  String get resolvedDialogTitle => dialogTitle ?? label;
}

Future<void> runWorkflowAction<T>(
  BuildContext context, {
  required WorkflowAction<T> action,
  required Future<void> Function(T trigger, {String? note}) onRun,
}) async {
  String? note;
  if (action.requiresNote) {
    note = await promptForReason(context, title: action.resolvedDialogTitle);
    if (note == null) return;
  } else if (action.confirmMessage != null) {
    final confirmed = await confirmDialog(
      context: context,
      title: action.resolvedDialogTitle,
      message: action.confirmMessage!,
    );
    if (confirmed != true) return;
  }

  if (!context.mounted) return;
  try {
    await onRun(action.trigger, note: note);
  } catch (e) {
    if (context.mounted) {
      ErrorBanner.show(context, message: userMessage(e));
    }
  }
}

class WorkflowActionButtons<T> extends StatefulWidget {
  final List<WorkflowAction<T>> actions;
  final Future<void> Function(T trigger, {String? note}) onRun;
  final bool compact;

  const WorkflowActionButtons({
    super.key,
    required this.actions,
    required this.onRun,
    this.compact = false,
  });

  @override
  State<WorkflowActionButtons<T>> createState() =>
      _WorkflowActionButtonsState<T>();
}

class _WorkflowActionButtonsState<T> extends State<WorkflowActionButtons<T>> {
  T? _busyTrigger;

  Future<void> _run(WorkflowAction<T> action) {
    return runWorkflowAction<T>(
      context,
      action: action,
      onRun: (trigger, {note}) async {
        setState(() => _busyTrigger = trigger);
        try {
          await widget.onRun(trigger, note: note);
        } finally {
          if (mounted) setState(() => _busyTrigger = null);
        }
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    if (widget.actions.isEmpty) return const SizedBox.shrink();
    return widget.compact ? _buildCompact() : _buildRegular();
  }

  Widget _buildRegular() {
    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      children: [
        for (final action in widget.actions)
          ElevatedButton.icon(
            onPressed: _busyTrigger == null ? () => _run(action) : null,
            style: ElevatedButton.styleFrom(
              foregroundColor: action.tone?.color,
            ),
            icon: _busyTrigger == action.trigger
                ? _spinner()
                : Icon(action.icon),
            label: Text(action.label),
          ),
      ],
    );
  }

  Widget _buildCompact() {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        for (final action in widget.actions) ...[
          TextButton.icon(
            onPressed: _busyTrigger == null ? () => _run(action) : null,
            style: TextButton.styleFrom(
              foregroundColor: action.tone?.color,
              visualDensity: VisualDensity.compact,
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm),
              minimumSize: const Size(0, 32),
              tapTargetSize: MaterialTapTargetSize.shrinkWrap,
            ),
            icon: _busyTrigger == action.trigger
                ? _spinner()
                : Icon(action.icon, size: 16),
            label: Text(action.label),
          ),
          if (action != widget.actions.last)
            const SizedBox(width: AppSpacing.xs),
        ],
      ],
    );
  }

  Widget _spinner() {
    return const SizedBox(
      width: AppSize.spinnerSm,
      height: AppSize.spinnerSm,
      child: CircularProgressIndicator(strokeWidth: 2),
    );
  }
}
