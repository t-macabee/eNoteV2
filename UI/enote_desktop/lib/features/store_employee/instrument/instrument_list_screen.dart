import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import 'instrument_form_screen.dart';
import 'instrument_provider.dart';

class InstrumentListScreen extends StatefulWidget {
  const InstrumentListScreen({super.key});

  @override
  State<InstrumentListScreen> createState() => _InstrumentListScreenState();
}

class _InstrumentListScreenState extends State<InstrumentListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<InstrumentDto>>();

  Future<void> _openForm([InstrumentDto? existing]) async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => InstrumentFormScreen(existing: existing),
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<InstrumentDto>(
      key: _listKey,
      config: EntityListConfig<InstrumentDto>(
        title: 'Instrumenti',
        columns: [
          ColumnSpec<InstrumentDto>(
            label: 'Model',
            value: (item) => item.model,
          ),
          ColumnSpec<InstrumentDto>(
            label: 'Proizvođač',
            value: (item) => item.manufacturer,
          ),
          ColumnSpec<InstrumentDto>(
            label: 'Tip instrumenta',
            value: (item) => item.instrumentType,
          ),
          ColumnSpec<InstrumentDto>(
            label: 'Dostupan',
            value: (item) => item.isAvailable ? 'Da' : 'Ne',
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<InstrumentProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'model': search,
        }),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          await context.read<InstrumentProvider>().remove(item.id);
          return true;
        },
      ),
    );
  }
}
