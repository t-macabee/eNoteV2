import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/paged_list_view.dart';
import 'course_provider.dart';

/// S14 — course catalogue (tab 1 › Kursevi): paged list with a
/// *Moji kursevi* (`enrolledOnly`) toggle (02 §5 S14).
class CourseListScreen extends StatefulWidget {
  const CourseListScreen({super.key});

  @override
  State<CourseListScreen> createState() => _CourseListScreenState();
}

class _CourseListScreenState extends State<CourseListScreen>
    with AutomaticKeepAliveClientMixin {
  late final PagedFetchController<CourseDto> _controller;
  bool _enrolledOnly = false;
  Object? _error;

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<CourseDto>(
      fetcher: _fetch,
      pageSize: 20,
      onError: (e) {
        if (mounted) setState(() => _error = e);
      },
    );
  }

  Future<PagedResult<CourseDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    final result = await context.read<CourseProvider>().getPage(
      params: {
        'page': page,
        'pageSize': pageSize,
        'includeTotalCount': true,
        if (search.isNotEmpty) 'name': search,
        if (_enrolledOnly) 'enrolledOnly': true,
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
    return PagedListView<CourseDto>(
      controller: _controller,
      error: _error,
      onRetry: () {
        setState(() => _error = null);
        _controller.refresh();
      },
      filterRow: SwitchListTile(
        title: const Text('Moji kursevi'),
        value: _enrolledOnly,
        onChanged: (value) {
          setState(() => _enrolledOnly = value);
          _controller.refresh(resetPage: true);
        },
      ),
      itemBuilder: (context, course) => _CourseRow(course: course),
    );
  }
}

class _CourseRow extends StatelessWidget {
  final CourseDto course;

  const _CourseRow({required this.course});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: const Icon(Icons.school_outlined),
      title: Text(course.name),
      subtitle: Text(
        '${orDash(course.instructorName)} · '
        '${formatDateNullable(course.startDate)}–'
        '${formatDateNullable(course.endDate)}',
      ),
      trailing: course.isEnrolled
          ? const Chip(
              label: Text('Upisan'),
              visualDensity: VisualDensity.compact,
            )
          : null,
      onTap: () => Navigator.of(context).pushNamed(
        AppRouter.courseDetail,
        arguments: CourseDetailArgs(course.id),
      ),
    );
  }
}
