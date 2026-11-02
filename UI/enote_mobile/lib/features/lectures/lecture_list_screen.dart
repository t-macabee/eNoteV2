import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../widgets/paged_list_view.dart';
import 'lecture_provider.dart';
import 'lecture_tile.dart';

class LectureListScreen extends StatefulWidget {
  const LectureListScreen({super.key});

  @override
  State<LectureListScreen> createState() => _LectureListScreenState();
}

class _LectureListScreenState extends State<LectureListScreen>
    with AutomaticKeepAliveClientMixin {
  late final PagedFetchController<LectureDto> _controller;
  LectureFilter _filter = LectureFilter.upcoming;
  int? _upcomingCount;

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<LectureDto>(
      fetcher: _fetch,
      pageSize: 20,
    );
    _loadUpcomingCount();
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
        filters: _filter.query(DateTime.now()),
      ),
    );
  }

  Future<void> _loadUpcomingCount() async {
    try {
      final count = await context.read<LectureProvider>().countUpcoming();
      if (mounted) setState(() => _upcomingCount = count);
    } catch (_) {
      if (mounted) setState(() => _upcomingCount = null);
    }
  }

  void _select(LectureFilter filter) {
    setState(() => _filter = filter);
    _controller.refresh(resetPage: true);
  }

  Future<void> _openDetail(LectureDto lecture) async {
    await Navigator.of(context).pushNamed(
      AppRoutes.lectureDetail,
      arguments: LectureDetailArgs(lecture.id),
    );
    if (!mounted) return;
    _controller.refresh(resetPage: true);
    _loadUpcomingCount();
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
      onRefresh: _loadUpcomingCount,
      statusTabs: StatusFilterTabs<LectureFilter>(
        scrollable: true,
        selected: _filter,
        onChanged: _select,
        options: [
          StatusFilterOption<LectureFilter>(
            label: 'Predstojeća',
            value: LectureFilter.upcoming,
            count: _upcomingCount,
          ),
          const StatusFilterOption<LectureFilter>(
            label: 'Sva',
            value: LectureFilter.all,
          ),
        ],
      ),
      itemBuilder: (context, lecture) =>
          LectureTile(lecture: lecture, onTap: () => _openDetail(lecture)),
    );
  }
}
