import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import 'address_form_screen.dart';
import 'address_provider.dart';

class AddressListScreen extends StatefulWidget {
  const AddressListScreen({super.key});

  @override
  State<AddressListScreen> createState() => _AddressListScreenState();
}

class _AddressListScreenState extends State<AddressListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<AddressReferenceDto>>();

  Future<void> _openForm([AddressReferenceDto? existing]) async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => AddressFormScreen(existing: existing),
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<AddressReferenceDto>(
      key: _listKey,
      config: EntityListConfig<AddressReferenceDto>(
        title: 'Adrese',
        columns: [
          ColumnSpec<AddressReferenceDto>(
            label: 'Grad',
            value: (item) => item.city,
          ),
          ColumnSpec<AddressReferenceDto>(
            label: 'Ulica',
            value: (item) => item.street,
          ),
          ColumnSpec<AddressReferenceDto>(
            label: 'Broj',
            value: (item) => item.number,
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<AddressProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'city': search,
        }),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          await context.read<AddressProvider>().remove(item.id);
          return true;
        },
      ),
    );
  }
}
