import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

typedef ColumnValueBuilder<T> = dynamic Function(T item);
typedef ColumnCellBuilder<T> = Widget Function(BuildContext context, T item);

class ColumnSpec<T> {
  final String label;
  final ColumnValueBuilder<T> value;
  final TextStyle? Function(T item)? style;
  final ColumnCellBuilder<T>? cellBuilder;
  final double? maxWidth;

  ColumnSpec({
    required this.label,
    required this.value,
    this.style,
    this.cellBuilder,
    this.maxWidth,
  });
}

typedef EntityFetcher<T> = Future<PagedResult<T>> Function(
  int page,
  int pageSize,
  String search,
);

enum EntityListPresentation { page, embedded }

enum EntityListStyle { table, tiles }

class EntityListConfig<T> {
  final String? title;
  final List<ColumnSpec<T>> columns;
  final String searchHint;
  final EntityFetcher<T> fetcher;
  final void Function(BuildContext context, T item)? onEdit;
  final Future<bool?> Function(BuildContext context, T item)? onDelete;
  final List<Widget> Function(BuildContext context, T item)? extraActions;
  final VoidCallback? onAdd;
  final Widget? trailing;
  final String? addLabel;
  final bool showAddButton;
  final bool showDeleteConfirmation;
  final String Function(T item)? deleteConfirmMessageOf;
  final String? deletedMessage;
  final bool showSearchBar;
  final Widget? filterBar;

  final Widget? statusTabs;

  final EntityListPresentation presentation;

  final EntityListStyle listStyle;

  final IconData? rowIcon;

  final List<int>? tileSubtitleColumns;

  final void Function(BuildContext context, T item)? onRowTap;

  final Color? Function(T item)? rowColor;

  const EntityListConfig({
    this.title,
    required this.columns,
    required this.fetcher,
    this.searchHint = 'Pretraži...',
    this.onEdit,
    this.onDelete,
    this.extraActions,
    this.onAdd,
    this.trailing,
    this.addLabel = 'Dodaj',
    this.showAddButton = true,
    this.showDeleteConfirmation = true,
    this.deleteConfirmMessageOf,
    this.deletedMessage,
    this.showSearchBar = true,
    this.filterBar,
    this.statusTabs,
    this.presentation = EntityListPresentation.page,
    this.listStyle = EntityListStyle.table,
    this.rowIcon,
    this.tileSubtitleColumns,
    this.onRowTap,
    this.rowColor,
  });
}
