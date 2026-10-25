import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/date_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import 'event_form_screen.dart';
import 'event_provider.dart';

class EventListScreen extends StatefulWidget {
  const EventListScreen({super.key});

  @override
  State<EventListScreen> createState() => _EventListScreenState();
}

class _EventListScreenState extends State<EventListScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<EventDto>>();

  DateTime? _from;
  DateTime? _to;

  Future<void> _openForm([EventDto? existing]) async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => EventFormScreen(
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _gridKey.currentState?.refresh();
  }

  void _applyFilters() {
    setState(() {});
    _gridKey.currentState?.refresh(resetPage: true);
  }

  @override
  Widget build(BuildContext context) {
    return EntityGridScreen<EventDto>(
      key: _gridKey,
      config: EntityGridConfig<EventDto>(
        searchHint: 'Pretraži po nazivu...',
        placeholderIcon: Icons.event_outlined,
        titleOf: (item) => item.title,
        imageUrlOf: (item) => item.imagePath,
        subtitleOf: (item) => formatDateTime(item.startsAt),
        filterBar: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(
              width: 180,
              child: DateField(
                labelText: 'Od',
                initialValue: _from,
                onChanged: (value) {
                  _from = value;
                  _applyFilters();
                },
              ),
            ),
            const SizedBox(width: 12),
            SizedBox(
              width: 180,
              child: DateField(
                labelText: 'Do',
                initialValue: _to,
                onChanged: (value) {
                  _to = value;
                  _applyFilters();
                },
              ),
            ),
            const SizedBox(width: 12),
          ],
        ),
        fetcher: (page, pageSize, search) => context.read<EventProvider>().search(pagedQuery(page, pageSize, search, searchField: 'title', filters: {
          if (_from != null) 'from': _from,
          if (_to != null) 'to': _to,
})),
        onAdd: () => _openForm(),
        onTap: (context, item) => _openForm(item),
      ),
    );
  }
}
