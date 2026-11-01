import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

class DashboardPage extends StatelessWidget {
  final List<Widget> cards;
  final String? storeName;

  const DashboardPage({super.key, required this.cards, this.storeName});

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
            ],
          ),
        ),
      ),
    );
  }
}
