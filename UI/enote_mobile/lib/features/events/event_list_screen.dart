import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/app_date_picker_field.dart';
import '../../widgets/paged_list_view.dart';
import 'event_provider.dart';

/// S27 — event list (tab 1 · Događaji): two date pickers → `from` / `to`
/// (02 §5 S27). The category label is derived from `courseId == null` only —
/// never `isPlatformWide` (01 A9): an uncoursed event with an instructor is
/// still *Škola*.
class EventListScreen extends StatefulWidget {
  const EventListScreen({super.key});

  @override
  State<EventListScreen> createState() => _EventListScreenState();
}

class _EventListScreenState extends State<EventListScreen>
    with AutomaticKeepAliveClientMixin {
  late final PagedFetchController<EventDto> _controller;
  DateTime? _from;
  DateTime? _to;
  Object? _error;

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<EventDto>(
      fetcher: _fetch,
      pageSize: 20,
      onError: (e) {
        if (mounted) setState(() => _error = e);
      },
    );
  }

  Future<PagedResult<EventDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    final result = await context.read<EventProvider>().getPage(
      params: {
        'page': page,
        'pageSize': pageSize,
        'includeTotalCount': true,
        if (_from != null) 'from': _from!.toIso8601String(),
        if (_to != null) 'to': _to!.toIso8601String(),
      },
    );
    if (mounted) setState(() => _error = null);
    return result;
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
          child: Row(
            children: [
              Expanded(
                child: AppDatePickerField(
                  label: 'Od',
                  value: _from,
                  onChanged: (value) {
                    setState(() => _from = value);
                    _controller.refresh(resetPage: true);
                  },
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: AppDatePickerField(
                  label: 'Do',
                  value: _to,
                  onChanged: (value) {
                    setState(() => _to = value);
                    _controller.refresh(resetPage: true);
                  },
                ),
              ),
            ],
          ),
        ),
        Expanded(
          child: PagedListView<EventDto>(
            controller: _controller,
            showSearch: false,
            error: _error,
            onRetry: () {
              setState(() => _error = null);
              _controller.refresh();
            },
            itemBuilder: (context, event) => _EventRow(event: event),
          ),
        ),
      ],
    );
  }
}

/// Category label for the student feed (01 A9): `courseId == null` is
/// platform-wide, regardless of `instructorId`.
String eventCategoryLabel(EventDto event) {
  if (event.courseId == null) return 'Škola';
  return event.courseName ?? '—';
}

class _EventRow extends StatelessWidget {
  final EventDto event;

  const _EventRow({required this.event});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: const Icon(Icons.event_outlined),
      title: Text(event.title),
      subtitle: Text(
        '${formatDateTime(event.startsAt)} · ${eventCategoryLabel(event)}',
      ),
      onTap: () => Navigator.of(context).pushNamed(
        AppRouter.eventDetail,
        arguments: EventDetailArgs(event),
      ),
    );
  }
}
