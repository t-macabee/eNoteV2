import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../widgets/paged_list_view.dart';
import 'assignment_provider.dart';
import 'assignment_tile.dart';

class AssignmentListScreen extends StatefulWidget {
  final AssignmentFilter initialFilter;

  const AssignmentListScreen({
    super.key,
    this.initialFilter = AssignmentFilter.open,
  });

  @override
  State<AssignmentListScreen> createState() => _AssignmentListScreenState();
}

class _AssignmentListScreenState extends State<AssignmentListScreen>
    with AutomaticKeepAliveClientMixin {
  late final PagedFetchController<AssignmentDto> _controller;
  late AssignmentFilter _filter;
  int? _openCount;

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _filter = widget.initialFilter;
    _controller = PagedFetchController<AssignmentDto>(
      fetcher: _fetch,
      pageSize: 20,
    );
    _loadOpenCount();
  }

  Future<PagedResult<AssignmentDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    return context.read<AssignmentProvider>().getPage(
      params: pagedQuery(
        page,
        pageSize,
        search,
        searchField: 'title',
        filters: _filter.query(DateTime.now()),
      ),
    );
  }

  Future<void> _loadOpenCount() async {
    try {
      final count = await context.read<AssignmentProvider>().countOpen();
      if (mounted) setState(() => _openCount = count);
    } catch (_) {
      if (mounted) setState(() => _openCount = null);
    }
  }

  void _select(AssignmentFilter filter) {
    setState(() => _filter = filter);
    _controller.refresh(resetPage: true);
  }

  Future<void> _openDetail(AssignmentDto assignment) async {
    await Navigator.of(context).pushNamed(
      AppRoutes.assignmentDetail,
      arguments: AssignmentDetailArgs(assignment.id),
    );
    if (!mounted) return;
    _controller.refresh(resetPage: true);
    _loadOpenCount();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    return PagedListView<AssignmentDto>(
      controller: _controller,
      onRefresh: _loadOpenCount,
      statusTabs: StatusFilterTabs<AssignmentFilter>(
        scrollable: true,
        selected: _filter,
        onChanged: _select,
        options: [
          StatusFilterOption<AssignmentFilter>(
            label: 'Otvoreni',
            value: AssignmentFilter.open,
            count: _openCount,
          ),
          const StatusFilterOption<AssignmentFilter>(
            label: 'Predani',
            value: AssignmentFilter.submitted,
          ),
          const StatusFilterOption<AssignmentFilter>(
            label: 'Rok istekao',
            value: AssignmentFilter.pastDue,
          ),
          const StatusFilterOption<AssignmentFilter>(
            label: 'Svi',
            value: AssignmentFilter.all,
          ),
        ],
      ),
      itemBuilder: (context, assignment) => AssignmentTile(
        assignment: assignment,
        onTap: () => _openDetail(assignment),
      ),
    );
  }
}
