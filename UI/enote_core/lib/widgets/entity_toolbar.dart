import 'package:flutter/material.dart';

import '../theme/app_tokens.dart';

class EntityToolbar extends StatelessWidget {
  const EntityToolbar({
    super.key,
    required this.searchController,
    required this.showSearch,
    required this.searchHint,
    required this.showAdd,
    this.filterBar,
    this.statusTabs,
    this.trailing,
    this.onAdd,
    this.addLabel,
  });

  final TextEditingController searchController;
  final bool showSearch;
  final String searchHint;
  final bool showAdd;
  final Widget? filterBar;
  final Widget? statusTabs;
  final Widget? trailing;
  final VoidCallback? onAdd;
  final String? addLabel;

  @override
  Widget build(BuildContext context) {
    if (!showSearch &&
        filterBar == null &&
        statusTabs == null &&
        !showAdd &&
        trailing == null) {
      return const SizedBox.shrink();
    }

    final searchField = TextField(
      controller: searchController,
      decoration: InputDecoration(
        hintText: searchHint,
        prefixIcon: const Icon(Icons.search),
        isDense: true,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppRadius.md),
        ),
      ),
    );

    final hasTopRow =
        showSearch || filterBar != null || showAdd || trailing != null;

    return Padding(
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (hasTopRow)
            IntrinsicHeight(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        if (showSearch)
                          Flexible(
                            child: filterBar != null
                                ? SizedBox(width: 320, child: searchField)
                                : searchField,
                          ),
                        if (showSearch && filterBar != null)
                          const SizedBox(width: AppSpacing.md),
                        ?filterBar,
                      ],
                    ),
                  ),
                  if (showAdd || trailing != null)
                    const SizedBox(width: AppSpacing.md),
                  if (trailing != null) ...[
                    trailing!,
                    const SizedBox(width: AppSpacing.md),
                  ],
                  if (showAdd)
                    ElevatedButton.icon(
                      onPressed: onAdd,
                      icon: const Icon(Icons.add),
                      label: Text(addLabel ?? 'Dodaj'),
                    ),
                ],
              ),
            ),
          if (hasTopRow && statusTabs != null)
            const SizedBox(height: AppSpacing.md),
          ?statusTabs,
        ],
      ),
    );
  }
}
