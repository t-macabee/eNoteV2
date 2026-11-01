import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

class DashboardShortcut {
  final String label;
  final IconData icon;
  final VoidCallback onTap;

  const DashboardShortcut({
    required this.label,
    required this.icon,
    required this.onTap,
  });
}

class DashboardPage extends StatelessWidget {
  final List<Widget> cards;
  final List<DashboardShortcut> shortcuts;
  final String? storeName;

  const DashboardPage({
    super.key,
    required this.cards,
    required this.shortcuts,
    this.storeName,
  });

  @override
  Widget build(BuildContext context) {
    final username = context.watch<AuthState>().username;
    final greeting = username == null || username.isEmpty
        ? 'Dobrodošli'
        : 'Dobrodošli, $username';
    final name = storeName;
    final welcome = name == null || name.isEmpty
        ? greeting
        : '$greeting · $name';

    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.xl),
      child: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: AppSize.contentMaxWidth),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(welcome, style: Theme.of(context).textTheme.headlineSmall),
              const SizedBox(height: AppSpacing.xl),
              Wrap(
                spacing: AppSpacing.lg,
                runSpacing: AppSpacing.lg,
                children: cards,
              ),
              const SizedBox(height: AppSpacing.xxl),
              Text('Prečice', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: AppSpacing.md),
              Wrap(
                spacing: AppSpacing.md,
                runSpacing: AppSpacing.md,
                children: [
                  for (final shortcut in shortcuts)
                    OutlinedButton.icon(
                      onPressed: shortcut.onTap,
                      icon: Icon(shortcut.icon, size: AppSize.spinnerSm),
                      label: Text(shortcut.label),
                    ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
