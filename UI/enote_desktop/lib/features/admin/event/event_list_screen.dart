import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/date_field.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../instructor/instructor_provider.dart';
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
  int? _instructorId;

  Future<void> _openForm([EventDto? existing]) async {
    if (existing != null && existing.isScoped) {
      ErrorBanner.show(
        context,
        message:
            'Administrator može upravljati samo događajima na nivou platforme.',
      );
      return;
    }
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
        subtitleOf: (item) {
          final time = formatDateTime(item.startsAt);
          if (item.isScoped) {
            return '$time • Samo pregled';
          }
          return time;
        },
        badgeOf: (item) => item.isScoped ? 'Samo pregled' : null,
        filterBar: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(
              width: 280,
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
              width: 280,
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
            SizedBox(
              width: 280,
              child: AsyncDropdown<InstructorDto>(
                label: 'Instruktor',
                fetcher: () async {
                  final result = await context
                      .read<InstructorProvider>()
                      .search({
                    'page': 1,
                    'pageSize': 200,
                    'includeTotalCount': false,
                  });
                  return result.items;
                },
                itemLabel: (instructor) {
                  final name =
                      '${instructor.firstName ?? ''} ${instructor.lastName ?? ''}'
                          .trim();
                  return name.isNotEmpty
                      ? name
                      : (instructor.username ?? '-');
                },
                itemId: (instructor) => instructor.id,
                value: _instructorId,
                onChanged: (id, _) {
                  _instructorId = id as int?;
                  _applyFilters();
                },
              ),
            ),
          ],
        ),
        fetcher: (page, pageSize, search) =>
            context.read<EventProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'title': search,
          if (_from != null) 'from': _from,
          if (_to != null) 'to': _to,
          if (_instructorId != null) 'instructorId': _instructorId,
        }),
        onAdd: () => _openForm(),
        onTap: (context, item) {
          if (item.isScoped) {
            ErrorBanner.show(
              context,
              message:
                  'Administrator može upravljati samo događajima na nivou platforme.',
            );
            return;
          }
          _openForm(item);
        },
        onDelete: (context, item) async {
          if (item.isScoped) {
            ErrorBanner.show(
              context,
              message:
                  'Administrator može upravljati samo događajima na nivou platforme.',
            );
            return false;
          }
          await context.read<EventProvider>().remove(item.id);
          return true;
        },
      ),
    );
  }
}
