import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/paged_list_view.dart';
import 'assignment_provider.dart';

/// S22 — assignment list (tab 1 · Zadaci): paged list with a *Rok istekao*
/// (`dueBefore = now`) toggle (02 §5 S22).
///
/// Tab bodies share the Učenje AppBar, so the *Moje predaje* entry to S24 is
/// a trailing action above the list rather than an AppBar button.
class AssignmentListScreen extends StatefulWidget {
  const AssignmentListScreen({super.key});

  @override
  State<AssignmentListScreen> createState() => _AssignmentListScreenState();
}

class _AssignmentListScreenState extends State<AssignmentListScreen>
    with AutomaticKeepAliveClientMixin {
  late final PagedFetchController<AssignmentDto> _controller;
  bool _pastDueOnly = false;
  Object? _error;

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<AssignmentDto>(
      fetcher: _fetch,
      pageSize: 20,
      onError: (e) {
        if (mounted) setState(() => _error = e);
      },
    );
  }

  Future<PagedResult<AssignmentDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    final result = await context.read<AssignmentProvider>().getPage(
      params: {
        'page': page,
        'pageSize': pageSize,
        'includeTotalCount': true,
        if (search.isNotEmpty) 'title': search,
        if (_pastDueOnly) 'dueBefore': DateTime.now().toIso8601String(),
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
        Align(
          alignment: Alignment.centerRight,
          child: TextButton(
            onPressed: () =>
                Navigator.of(context).pushNamed(AppRouter.assignmentHistory),
            child: const Text('Moje predaje'),
          ),
        ),
        Expanded(
          child: PagedListView<AssignmentDto>(
            controller: _controller,
            error: _error,
            onRetry: () {
              setState(() => _error = null);
              _controller.refresh();
            },
            filterRow: SwitchListTile(
              title: const Text('Rok istekao'),
              value: _pastDueOnly,
              onChanged: (value) {
                setState(() => _pastDueOnly = value);
                _controller.refresh(resetPage: true);
              },
            ),
            itemBuilder: (context, assignment) =>
                _AssignmentRow(assignment: assignment),
          ),
        ),
      ],
    );
  }
}

class _AssignmentRow extends StatelessWidget {
  final AssignmentDto assignment;

  const _AssignmentRow({required this.assignment});

  @override
  Widget build(BuildContext context) {
    final pastDue = DateTime.now().isAfter(assignment.dueAt);
    return ListTile(
      leading: const Icon(Icons.assignment_outlined),
      title: Text(assignment.title),
      subtitle: Text('Rok: ${formatDateTime(assignment.dueAt)}'),
      trailing: pastDue ? const Chip(label: Text('Rok istekao')) : null,
      onTap: () => Navigator.of(context).pushNamed(
        AppRouter.assignmentDetail,
        arguments: AssignmentDetailArgs(assignment.id),
      ),
    );
  }
}
