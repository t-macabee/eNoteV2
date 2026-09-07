import 'package:flutter/material.dart';

/// The single-row toolbar shared by [EntityGridScreen] and the inline-toolbar
/// mode of [EntityListScreen]: search field, filter bar, trailing widget and
/// Add button, all on one line at one height.
///
/// This covers the *inline* layout only. [EntityListScreen] also has a stacked
/// layout — a full-width search bar above the filter bar, with Add living in
/// the AppBar actions — which is what its default (non-inline, non-embedded)
/// consumers use. That path has no grid counterpart and stays in
/// [EntityListScreen].
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
            // Search + filterBar share one Expanded slot so they're the
            // only thing that gives way when the row is tight (the search
            // field shrinks below its 420 cap instead of overflowing).
            // Keeping them out of the outer Row's flex pool — rather than
            // giving the search field its own Flexible there — means they
            // don't compete with trailing/the add button for space: any
            // width they don't use here stays inside this Expanded instead
            // of leaking past the button as unused trailing space.
            Expanded(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (showSearch)
                    Flexible(
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 420),
                        child: TextField(
                          controller: searchController,
                          decoration: InputDecoration(
                            hintText: searchHint,
                            prefixIcon: const Icon(Icons.search),
                            border: const OutlineInputBorder(),
                          ),
                        ),
                      ),
                    ),
                  if (showSearch && filterBar != null)
                    const SizedBox(width: 12),
                  // Placed directly (no Expanded/Flexible): filterBar must
                  // size itself to its own content (e.g. a Row with
                  // MainAxisSize.min, or a fixed-width SizedBox).
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
