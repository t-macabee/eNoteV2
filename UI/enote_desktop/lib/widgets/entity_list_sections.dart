import 'package:flutter/material.dart';

import 'entity_list_screen.dart';

/// Shared edit/delete action row used by both list row renderers.
class EntityRowActions extends StatelessWidget {
  final List<Widget> extraWidgets;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;

  const EntityRowActions({
    super.key,
    this.extraWidgets = const <Widget>[],
    this.onEdit,
    this.onDelete,
  });

  @override
  Widget build(BuildContext context) {
    final hasEditDelete = onEdit != null || onDelete != null;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        ...extraWidgets,
        if (extraWidgets.isNotEmpty && hasEditDelete)
          const SizedBox(
            height: 20,
            child: VerticalDivider(width: 12),
          ),
        if (onEdit != null)
          IconButton(
            icon: const Icon(Icons.edit, size: 18),
            onPressed: onEdit,
          ),
        if (onDelete != null)
          IconButton(
            icon: const Icon(Icons.delete, size: 18, color: Colors.red),
            onPressed: onDelete,
          ),
      ],
    );
  }
}

class EntityListTiles<T> extends StatelessWidget {
  final EntityListConfig<T> config;
  final List<T> items;
  final void Function(T item) onDelete;

  const EntityListTiles({
    super.key,
    required this.config,
    required this.items,
    required this.onDelete,
  });

  @override
  Widget build(BuildContext context) {
    final hasActions =
        config.onEdit != null ||
        config.onDelete != null ||
        config.extraActions != null;
    final isEmbedded = config.presentation == EntityListPresentation.embedded;

    return ListView.separated(
      shrinkWrap: isEmbedded,
      itemCount: items.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final item = items[index];
        final columns = config.columns;
        final title = columns.isNotEmpty
            ? (columns.first.value(item)?.toString() ?? '-')
            : '';

        String? subtitle;
        Widget? subtitleWidget;
        final subtitleIndices = config.tileSubtitleColumns;
        List<ColumnSpec<T>> subtitleCols;
        if (subtitleIndices != null) {
          subtitleCols = <ColumnSpec<T>>[];
          for (final i in subtitleIndices) {
            if (i < 0 || i >= columns.length) continue;
            subtitleCols.add(columns[i]);
          }
        } else if (columns.length > 1) {
          subtitleCols = columns.skip(1).toList();
        } else {
          subtitleCols = <ColumnSpec<T>>[];
        }
        if (subtitleCols.isNotEmpty) {
          final hasCustom = subtitleCols.any((col) => col.cellBuilder != null);
          if (!hasCustom) {
            final parts = <String>[];
            for (final col in subtitleCols) {
              final val = col.value(item)?.toString() ?? '-';
              parts.add('${col.label}: $val');
            }
            if (parts.isNotEmpty) subtitle = parts.join(' · ');
          } else {
            subtitleWidget = Wrap(
              spacing: 8,
              runSpacing: 4,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                for (final col in subtitleCols)
                  if (col.cellBuilder != null)
                    col.cellBuilder!(context, item)
                  else
                    Text('${col.label}: ${col.value(item)?.toString() ?? '-'}'),
              ],
            );
          }
        }

        final onRowTap = config.onRowTap ?? config.onEdit;
        final extraWidgets =
            config.extraActions?.call(context, item) ?? const <Widget>[];
        return ListTile(
          leading: config.rowIcon != null ? Icon(config.rowIcon) : null,
          title: Text(
            title,
            style: columns.isNotEmpty ? columns.first.style?.call(item) : null,
          ),
          subtitle: subtitleWidget ?? (subtitle != null ? Text(subtitle) : null),
          onTap: onRowTap != null ? () => onRowTap(context, item) : null,
          trailing: hasActions
              ? EntityRowActions(
                  extraWidgets: extraWidgets,
                  onEdit: config.onEdit != null
                      ? () => config.onEdit!(context, item)
                      : null,
                  onDelete: config.onDelete != null
                      ? () => onDelete(item)
                      : null,
                )
              : null,
        );
      },
    );
  }
}

class EntityListTable<T> extends StatelessWidget {
  final EntityListConfig<T> config;
  final List<T> items;
  final void Function(T item) onDelete;

  const EntityListTable({
    super.key,
    required this.config,
    required this.items,
    required this.onDelete,
  });

  @override
  Widget build(BuildContext context) {
    final hasActions =
        config.onEdit != null ||
        config.onDelete != null ||
        config.extraActions != null;

    return SingleChildScrollView(
      child: DataTable(
        columns: [
          ...config.columns.map((c) => DataColumn(label: Text(c.label))),
          if (hasActions) const DataColumn(label: Text('Akcije')),
        ],
        rows: items.map((item) {
          final extraWidgets =
              config.extraActions?.call(context, item) ?? const <Widget>[];
          return DataRow(
            cells: [
              ...config.columns.map(
                (col) => DataCell(
                  col.cellBuilder != null
                      ? col.cellBuilder!(context, item)
                      : Text(
                          col.value(item)?.toString() ?? '-',
                          style: col.style?.call(item),
                        ),
                  onTap: config.onEdit != null
                      ? () => config.onEdit!(context, item)
                      : null,
                ),
              ),
              if (hasActions)
                DataCell(
                  EntityRowActions(
                    extraWidgets: extraWidgets,
                    onEdit: config.onEdit != null
                        ? () => config.onEdit!(context, item)
                        : null,
                    onDelete: config.onDelete != null
                        ? () => onDelete(item)
                        : null,
                  ),
                ),
            ],
          );
        }).toList(),
      ),
    );
  }
}

/// The stacked (non-inline) search bar. List-only: the grid has no
/// counterpart, and this is the layout the default list screens use, with
/// their Add button in the AppBar actions above.
class EntityListSearchBar extends StatelessWidget {
  final TextEditingController controller;
  final String hint;

  const EntityListSearchBar({
    super.key,
    required this.controller,
    required this.hint,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Align(
        alignment: Alignment.centerLeft,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: TextField(
            controller: controller,
            decoration: InputDecoration(
              hintText: hint,
              prefixIcon: const Icon(Icons.search),
              border: const OutlineInputBorder(),
            ),
          ),
        ),
      ),
    );
  }
}
