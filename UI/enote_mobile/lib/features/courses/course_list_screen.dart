import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../widgets/paged_list_view.dart';
import 'course_provider.dart';
import 'course_tile.dart';

class CourseListScreen extends StatefulWidget {
  const CourseListScreen({super.key});

  @override
  State<CourseListScreen> createState() => _CourseListScreenState();
}

class _CourseListScreenState extends State<CourseListScreen>
    with AutomaticKeepAliveClientMixin {
  late final PagedFetchController<CourseDto> _controller;
  bool _enrolledOnly = false;

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<CourseDto>(
      fetcher: _fetch,
      pageSize: 20,
    );
  }

  Future<PagedResult<CourseDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    return context.read<CourseProvider>().getPage(
      params: pagedQuery(
        page,
        pageSize,
        search,
        searchField: 'name',
        filters: {if (_enrolledOnly) 'enrolledOnly': true},
      ),
    );
  }

  void _selectEnrolledOnly(bool enrolledOnly) {
    setState(() => _enrolledOnly = enrolledOnly);
    _controller.refresh(resetPage: true);
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
      statusTabs: StatusFilterTabs<bool>(
        scrollable: true,
        selected: _enrolledOnly,
        onChanged: _selectEnrolledOnly,
        options: const [
          StatusFilterOption<bool>(label: 'Moji', value: true),
          StatusFilterOption<bool>(label: 'Svi', value: false),
        ],
      ),
      itemBuilder: (context, course) => CourseTile(
        course: course,
        onTap: () => Navigator.of(context).pushNamed(
          AppRoutes.courseDetail,
          arguments: CourseDetailArgs(course.id),
        ),
      ),
    );
  }
}
