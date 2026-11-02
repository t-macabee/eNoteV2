import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class DetailActions extends StatelessWidget {
  final Widget? banner;
  final List<Widget> actions;

  const DetailActions({super.key, this.banner, required this.actions});

  @override
  Widget build(BuildContext context) {
    final items = [?banner, ...actions];
    if (items.isEmpty) return const SizedBox.shrink();
    final children = <Widget>[];
    for (final item in items) {
      if (children.isNotEmpty) {
        children.add(const SizedBox(height: AppSpacing.sm));
      }
      children.add(item);
    }
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.xl,
        AppSpacing.lg,
        AppSpacing.lg,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: children,
      ),
    );
  }
}
