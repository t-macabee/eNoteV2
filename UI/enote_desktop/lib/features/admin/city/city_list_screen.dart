import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'city_form_screen.dart';
import 'city_provider.dart';

class CityListScreen extends StatefulWidget {
  final EntityListPresentation presentation;
  final EntityListStyle listStyle;
  final IconData? rowIcon;

  const CityListScreen({
    super.key,
    this.presentation = EntityListPresentation.page,
    this.listStyle = EntityListStyle.tiles,
    this.rowIcon = Icons.location_city,
  });

  @override
  State<CityListScreen> createState() => _CityListScreenState();
}

class _CityListScreenState extends State<CityListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<CityDto>>();

  Future<void> _openForm([CityDto? existing]) async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => CityFormScreen(
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<CityDto>(
      key: _listKey,
      config: EntityListConfig<CityDto>(
        title: 'Gradovi',
        presentation: widget.presentation,
        listStyle: widget.listStyle,
        rowIcon: widget.rowIcon,
        columns: [
          ColumnSpec<CityDto>(
            label: 'Naziv',
            value: (item) => item.name,
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<CityProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'name': search,
        }),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          await context.read<CityProvider>().remove(item.id);
          return true;
        },
      ),
    );
  }
}
