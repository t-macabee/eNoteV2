import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'instrument_type_form_screen.dart';
import 'instrument_type_provider.dart';

class InstrumentTypeListScreen extends StatefulWidget {
  final EntityListPresentation presentation;
  final EntityListStyle listStyle;
  final IconData? rowIcon;

  const InstrumentTypeListScreen({
    super.key,
    this.presentation = EntityListPresentation.page,
    this.listStyle = EntityListStyle.tiles,
    this.rowIcon = Icons.music_note,
  });

  @override
  State<InstrumentTypeListScreen> createState() =>
      _InstrumentTypeListScreenState();
}

class _InstrumentTypeListScreenState extends State<InstrumentTypeListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<InstrumentTypeDto>>();

  Future<void> _openForm([InstrumentTypeDto? existing]) async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => InstrumentTypeFormScreen(
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<InstrumentTypeDto>(
      key: _listKey,
      config: EntityListConfig<InstrumentTypeDto>(
        title: 'Tipovi instrumenata',
        presentation: widget.presentation,
        listStyle: widget.listStyle,
        rowIcon: widget.rowIcon,
        columns: [
          ColumnSpec<InstrumentTypeDto>(
            label: 'Tip',
            value: (item) => item.type,
          ),
          ColumnSpec<InstrumentTypeDto>(
            label: 'Mjesečna naknada',
            value: (item) => item.monthlyFee.toStringAsFixed(2),
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<InstrumentTypeProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'type': search,
        }),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          await context.read<InstrumentTypeProvider>().remove(item.id);
          return true;
        },
      ),
    );
  }
}
