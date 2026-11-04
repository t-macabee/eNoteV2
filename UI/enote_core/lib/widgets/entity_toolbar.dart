import 'dart:math';

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

  static const double _singleRowMinWidth = 700;

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

    final searchAndFilter = Row(
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
    );

    final addButton = showAdd
        ? ElevatedButton.icon(
            onPressed: onAdd,
            icon: const Icon(Icons.add),
            label: Text(addLabel ?? 'Dodaj'),
          )
        : null;

    return Padding(
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final singleRow = constraints.maxWidth >= _singleRowMinWidth;
          return Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (hasTopRow)
                if (singleRow)
                  IntrinsicHeight(
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Expanded(child: searchAndFilter),
                        if (showAdd || trailing != null)
                          const SizedBox(width: AppSpacing.md),
                        if (trailing != null) ...[
                          trailing!,
                          const SizedBox(width: AppSpacing.md),
                        ],
                        ?addButton,
                      ],
                    ),
                  )
                else ...[
                  if (showSearch || filterBar != null)
                    Wrap(
                      spacing: AppSpacing.md,
                      runSpacing: AppSpacing.sm,
                      children: [
                        if (showSearch)
                          SizedBox(
                            width: filterBar != null
                                ? min(320.0, constraints.maxWidth)
                                : constraints.maxWidth,
                            child: searchField,
                          ),
                        ?filterBar,
                      ],
                    ),
                  if ((showSearch || filterBar != null) &&
                      (trailing != null || addButton != null))
                    const SizedBox(height: AppSpacing.md),
                  if (trailing != null || addButton != null)
                    Wrap(
                      alignment: WrapAlignment.end,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      spacing: AppSpacing.md,
                      runSpacing: AppSpacing.sm,
                      children: [?trailing, ?addButton],
                    ),
                ],
              if (hasTopRow && statusTabs != null)
                const SizedBox(height: AppSpacing.md),
              ?statusTabs,
            ],
          );
        },
      ),
    );
  }
}
