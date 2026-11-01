import 'package:flutter/material.dart';

import '../theme/app_theme.dart';
import 'entity_list_config.dart';

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
          const SizedBox(height: 20, child: VerticalDivider(width: 12)),
        if (onEdit != null)
          IconButton(
            icon: const Icon(Icons.edit, size: 18),
            visualDensity: VisualDensity.compact,
            onPressed: onEdit,
          ),
        if (onDelete != null)
          IconButton(
            icon: const Icon(Icons.delete, size: 18, color: AppTheme.error),
            visualDensity: VisualDensity.compact,
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
          subtitle:
              subtitleWidget ?? (subtitle != null ? Text(subtitle) : null),
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

class EntityListTable<T> extends StatefulWidget {
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
  State<EntityListTable<T>> createState() => _EntityListTableState<T>();
}

class _EntityListTableState<T> extends State<EntityListTable<T>> {
  static const double _restingActionOpacity = 0.45;

  int? _hoveredIndex;

  void _setHoveredIndex(int? index) {
    if (_hoveredIndex == index) return;
    setState(() => _hoveredIndex = index);
  }

  Widget _trackRowHover(int index, Widget child) => MouseRegion(
    onEnter: (_) => _setHoveredIndex(index),
    onExit: (_) => _setHoveredIndex(null),
    child: child,
  );

  DataRow _buildRow(BuildContext context, int index) {
    final config = widget.config;
    final item = widget.items[index];
    final extraWidgets =
        config.extraActions?.call(context, item) ?? const <Widget>[];

    return DataRow(
      cells: [
        for (final col in config.columns)
          DataCell(
            _trackRowHover(
              index,
              col.cellBuilder != null
                  ? col.cellBuilder!(context, item)
                  : Text(
                      col.value(item)?.toString() ?? '-',
                      style: col.style?.call(item),
                    ),
            ),
            onTap: config.onEdit != null
                ? () => config.onEdit!(context, item)
                : null,
          ),
        if (_hasActions)
          DataCell(
            _trackRowHover(
              index,
              Opacity(
                opacity: _hoveredIndex == index ? 1 : _restingActionOpacity,
                child: EntityRowActions(
                  extraWidgets: extraWidgets,
                  onEdit: config.onEdit != null
                      ? () => config.onEdit!(context, item)
                      : null,
                  onDelete: config.onDelete != null
                      ? () => widget.onDelete(item)
                      : null,
                ),
              ),
            ),
          ),
      ],
    );
  }

  bool get _hasActions =>
      widget.config.onEdit != null ||
      widget.config.onDelete != null ||
      widget.config.extraActions != null;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: DataTable(
          dividerThickness: 1,
          headingRowColor: const WidgetStatePropertyAll(
            AppTheme.surfaceContainer,
          ),
          columns: [
            ...widget.config.columns.map(
              (c) => DataColumn(label: Text(c.label)),
            ),
            if (_hasActions) const DataColumn(label: Text('Akcije')),
          ],
          rows: [
            for (var index = 0; index < widget.items.length; index++)
              _buildRow(context, index),
          ],
        ),
      ),
    );
  }
}
