import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/paged_list_view.dart';
import 'assignment_provider.dart';
import 'file_picker_field.dart';

/// S24 — own submission history (02 §5 S24): paged list over
/// `AssignmentProvider.myHistory`, no search. Rows show names and dates
/// only, never ids. Tap opens the assignment (S23) for its grade.
class SubmissionHistoryScreen extends StatefulWidget {
  const SubmissionHistoryScreen({super.key});

  @override
  State<SubmissionHistoryScreen> createState() => _SubmissionHistoryScreenState();
}

class _SubmissionHistoryScreenState extends State<SubmissionHistoryScreen> {
  late final PagedFetchController<AssignmentSubmissionDto> _controller;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<AssignmentSubmissionDto>(
      fetcher: _fetch,
      pageSize: 20,
      onError: (e) {
        if (mounted) setState(() => _error = e);
      },
    );
  }

  Future<PagedResult<AssignmentSubmissionDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    final result = await context
        .read<AssignmentProvider>()
        .myHistory(page: page, pageSize: pageSize);
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
    return Scaffold(
      appBar: AppBar(title: const Text('Moje predaje')),
      body: PagedListView<AssignmentSubmissionDto>(
        controller: _controller,
        showSearch: false,
        error: _error,
        onRetry: () {
          setState(() => _error = null);
          _controller.refresh();
        },
        itemBuilder: (context, submission) =>
            _SubmissionRow(submission: submission),
      ),
    );
  }
}

class _SubmissionRow extends StatelessWidget {
  final AssignmentSubmissionDto submission;

  const _SubmissionRow({required this.submission});

  @override
  Widget build(BuildContext context) {
    final submittedAt = submission.submittedAt;
    return ListTile(
      leading: const Icon(Icons.upload_file_outlined),
      title: Text(
        submittedAt == null ? '—' : formatDateTime(submittedAt),
      ),
      subtitle: Text(
        assignmentFileName(submission.filePath),
      ),
      trailing: submission.grade == null
          ? const Chip(label: Text('Neocijenjeno'))
          : Chip(label: Text('Ocjena: ${submission.grade}')),
      onTap: () => Navigator.of(context).pushNamed(
        AppRouter.assignmentDetail,
        arguments: AssignmentDetailArgs(submission.assignmentId),
      ),
    );
  }
}
