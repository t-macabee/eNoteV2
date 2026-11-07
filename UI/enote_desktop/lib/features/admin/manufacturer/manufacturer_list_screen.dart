import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'manufacturer_form_screen.dart';
import 'manufacturer_provider.dart';

class ManufacturerListScreen extends StatefulWidget {
  final EntityListPresentation presentation;
  final EntityListStyle listStyle;
  final IconData? rowIcon;

  const ManufacturerListScreen({
    super.key,
    this.presentation = EntityListPresentation.page,
    this.listStyle = EntityListStyle.tiles,
    this.rowIcon = Icons.business_outlined,
  });

  @override
  State<ManufacturerListScreen> createState() => _ManufacturerListScreenState();
}

class _ManufacturerListScreenState extends State<ManufacturerListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<ManufacturerDto>>();

  Future<void> _openForm([ManufacturerDto? existing]) async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => ManufacturerFormScreen(
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<ManufacturerDto>(
      key: _listKey,
      config: EntityListConfig<ManufacturerDto>(
        title: 'Proizvođači',
        presentation: widget.presentation,
        listStyle: widget.listStyle,
        rowIcon: widget.rowIcon,
        columns: [
          ColumnSpec<ManufacturerDto>(
            label: 'Naziv',
            value: (item) => item.name,
          ),
        ],
        fetcher: (page, pageSize, search) => context
            .read<ManufacturerProvider>()
            .search(pagedQuery(page, pageSize, search, searchField: 'name')),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        deleteConfirmMessageOf: (item) =>
            'Da li ste sigurni da želite da obrišete proizvođača "${item.name}"?',
        deletedMessage: 'Proizvođač je obrisan.',
        onDelete: (context, item) async {
          await context.read<ManufacturerProvider>().remove(item.id);
          return true;
        },
      ),
    );
  }
}
