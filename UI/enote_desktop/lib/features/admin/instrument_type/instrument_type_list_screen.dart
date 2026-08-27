import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import 'instrument_type_form_screen.dart';
import 'instrument_type_provider.dart';

class InstrumentTypeListScreen extends StatefulWidget {
  const InstrumentTypeListScreen({super.key});

  @override
  State<InstrumentTypeListScreen> createState() =>
      _InstrumentTypeListScreenState();
}

class _InstrumentTypeListScreenState extends State<InstrumentTypeListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<InstrumentTypeDto>>();

  Future<void> _openForm([InstrumentTypeDto? existing]) async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => InstrumentTypeFormScreen(existing: existing),
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
