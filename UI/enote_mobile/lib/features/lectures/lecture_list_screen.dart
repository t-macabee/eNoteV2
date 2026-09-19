import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/paged_list_view.dart';
import '../../widgets/status_chip.dart';
import 'lecture_provider.dart';

/// S17 — lecture list (tab 1 › Predavanja): paged list with a
/// *Predstojeća* (`from = now`) toggle (02 §5 S17).
class LectureListScreen extends StatefulWidget {
  const LectureListScreen({super.key});

  @override
  State<LectureListScreen> createState() => _LectureListScreenState();
}

class _LectureListScreenState extends State<LectureListScreen>
    with AutomaticKeepAliveClientMixin {
  late final PagedFetchController<LectureDto> _controller;
  bool _upcomingOnly = false;

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<LectureDto>(
      fetcher: _fetch,
      pageSize: 20,
    );
  }

  Future<PagedResult<LectureDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    return context.read<LectureProvider>().getPage(
      params: pagedQuery(
        page,
        pageSize,
        search,
        searchField: 'name',
        filters: {
          if (_upcomingOnly) 'from': DateTime.now().toIso8601String(),
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
    return PagedListView<LectureDto>(
      controller: _controller,
      filterRow: SwitchListTile(
        title: const Text('Predstojeća'),
        value: _upcomingOnly,
        onChanged: (value) {
          setState(() => _upcomingOnly = value);
          _controller.refresh(resetPage: true);
        },
      ),
      itemBuilder: (context, lecture) => _LectureRow(lecture: lecture),
    );
  }
}

class _LectureRow extends StatelessWidget {
  final LectureDto lecture;

  const _LectureRow({required this.lecture});

  static IconData _iconFor(LectureType type) => switch (type) {
    LectureType.theoretical => Icons.school_outlined,
    LectureType.practical => Icons.music_note_outlined,
    LectureType.combined => Icons.event_note_outlined,
  };

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(_iconFor(lecture.lectureType)),
      title: Text(lecture.name),
      subtitle: Text(
        '${formatDateTime(lecture.lectureTime)} · ${lecture.location}',
      ),
      trailing: StatusChip.lectureStatus(lecture.lectureStatus),
      onTap: () => Navigator.of(context).pushNamed(
        AppRouter.lectureDetail,
        arguments: LectureDetailArgs(lecture.id),
      ),
    );
  }
}
