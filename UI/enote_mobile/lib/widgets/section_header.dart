import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class SectionHeader extends StatelessWidget {
  final String title;
  final Widget? trailing;

  const SectionHeader({super.key, required this.title, this.trailing});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.lg,
        vertical: AppSpacing.sm,
      ),
      child: Row(
        children: [
          Expanded(
            child: Text(
              title.toUpperCase(),
              style: Theme.of(context).textTheme.labelSmall,
            ),
          ),
          ?trailing,
        ],
      ),
    );
  }
}
