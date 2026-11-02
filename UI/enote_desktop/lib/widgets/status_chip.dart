import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

class StatusChip extends StatelessWidget {
  final String label;
  final StatusTone tone;

  const StatusChip({super.key, required this.label, required this.tone});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: tone.background,
        borderRadius: BorderRadius.circular(AppRadius.pill),
      ),
      child: Text(
        label,
        style: Theme.of(context).textTheme.labelMedium
            ?.copyWith(color: tone.color, fontWeight: FontWeight.w600),
      ),
    );
  }
}
