import 'package:flutter/material.dart';

class EntityToolbar extends StatelessWidget {
  const EntityToolbar({
    super.key,
    required this.searchController,
    required this.showSearch,
    required this.searchHint,
    required this.showAdd,
    this.filterBar,
    this.trailing,
    this.onAdd,
    this.addLabel,
  });

  final TextEditingController searchController;
  final bool showSearch;
  final String searchHint;
  final bool showAdd;
  final Widget? filterBar;
  final Widget? trailing;
  final VoidCallback? onAdd;
  final String? addLabel;

  @override
  Widget build(BuildContext context) {
    if (!showSearch && filterBar == null && !showAdd && trailing == null) {
      return const SizedBox.shrink();
    }

    return Padding(
      padding: const EdgeInsets.all(16),
      child: IntrinsicHeight(
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
                          ? ConstrainedBox(
                              constraints:
                                  const BoxConstraints(maxWidth: 420),
                              child: TextField(
                                controller: searchController,
                                decoration: InputDecoration(
                                  hintText: searchHint,
                                  prefixIcon: const Icon(Icons.search),
                                  border: const OutlineInputBorder(),
                                ),
                              ),
                            )
                          : TextField(
                              controller: searchController,
                              decoration: InputDecoration(
                                hintText: searchHint,
                                prefixIcon: const Icon(Icons.search),
                                border: const OutlineInputBorder(),
                              ),
                            ),
                    ),
                  if (showSearch && filterBar != null)
                    const SizedBox(width: 12),
                  ?filterBar,
                ],
              ),
            ),
            if (showAdd || trailing != null) const SizedBox(width: 12),
            if (trailing != null) ...[
              trailing!,
              const SizedBox(width: 12),
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
    );
  }
}
