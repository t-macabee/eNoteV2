import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../widgets/app_date_picker_field.dart';
import '../../widgets/paged_list_view.dart';
import 'event_provider.dart';

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

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<EventDto>(fetcher: _fetch, pageSize: 20);
  }

  Future<PagedResult<EventDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    return context.read<EventProvider>().getPage(
      params: pagedQuery(
        page,
        pageSize,
        search,
        filters: {
          if (_from != null) 'from': toUtcIso(_from!),
          if (_to != null)
            'to': toUtcIso(
              DateTime(_to!.year, _to!.month, _to!.day, 23, 59, 59),
            ),
        },
      ),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    return PagedListView<EventDto>(
      controller: _controller,
      showSearch: false,
      filterRow: Row(
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
          const SizedBox(width: AppSpacing.md),
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
      itemBuilder: (context, event) => _EventRow(event: event),
    );
  }
}

class _EventRow extends StatelessWidget {
  final EventDto event;

  const _EventRow({required this.event});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: const Icon(Icons.event_outlined),
      title: Text(event.title),
      subtitle: Text(formatDateTime(event.startsAt)),
      onTap: () => Navigator.of(context)
          .pushNamed(AppRoutes.eventDetail, arguments: EventDetailArgs(event)),
    );
  }
}
