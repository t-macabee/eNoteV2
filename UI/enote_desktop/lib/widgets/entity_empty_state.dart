import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

class EntityEmptyState extends StatelessWidget {
  final bool searching;

  const EntityEmptyState({super.key, required this.searching});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.inbox_outlined,
            size: 40,
            color: AppTheme.textTertiary,
          ),
          const SizedBox(height: AppSpacing.sm),
          Text(searching ? 'Nema rezultata za pretragu.' : 'Nema podataka.'),
        ],
      ),
    );
  }
}
