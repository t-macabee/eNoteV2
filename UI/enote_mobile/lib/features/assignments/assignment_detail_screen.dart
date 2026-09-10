import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/blocked_reason_banner.dart';
import '../../widgets/labeled_value.dart';
import '../../widgets/section_header.dart';
import 'assignment_provider.dart';
import 'file_picker_field.dart';

/// S23 — assignment detail with the file-submission section (02 §4.7).
///
/// Load = assignment ‖ own submission (02 D7); a 404 submission is state A.
/// The *Predavanje ›* row navigates by id but never renders it. Image
/// submissions preview inline with the bearer header (01 G13); PDFs show the
/// name only. The `( Rang lista kursa )` shortcut renders only when the route
/// args carry a [courseId] (`AssignmentDto` has none, 02 §12 item 5).
class AssignmentDetailScreen extends StatefulWidget {
  final int assignmentId;
  final int? courseId;

  /// Test seam forwarded to the [FilePickerField].
  final Future<PickedAssignmentFile?> Function()? pickFiles;

  const AssignmentDetailScreen({
    super.key,
    required this.assignmentId,
    this.courseId,
    this.pickFiles,
  });

  @override
  State<AssignmentDetailScreen> createState() => _AssignmentDetailScreenState();
}

class _AssignmentDetailScreenState extends State<AssignmentDetailScreen> {
  AssignmentDto? _assignment;
  AssignmentSubmissionDto? _submission;
  Object? _error;
  bool _loading = true;
  PickedAssignmentFile? _picked;
  bool _submitting = false;
  String? _submitError;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final provider = context.read<AssignmentProvider>();
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final results = await Future.wait<dynamic>([
        provider.getById(widget.assignmentId),
        provider.mySubmission(widget.assignmentId),
      ]);
      if (!mounted) return;
      setState(() {
        _assignment = results[0] as AssignmentDto;
        _submission = results[1] as AssignmentSubmissionDto?;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e;
        _loading = false;
      });
    }
  }

  Future<void> _submit() async {
    final picked = _picked;
    if (picked == null || _submitting) return;
    final confirmed = await confirmDialog(
      context: context,
      title: 'Predaja zadatka',
      message:
          'Predati "${picked.fileName}"? Predaju nije moguće izmijeniti.',
    );
    if (confirmed != true || !mounted) return;
    final provider = context.read<AssignmentProvider>();
    final messenger = ScaffoldMessenger.of(context);
    setState(() {
      _submitting = true;
      _submitError = null;
    });
    try {
      await provider.submit(
        widget.assignmentId,
        picked.bytes,
        picked.fileName,
        picked.contentType,
      );
      final submission = await provider.mySubmission(widget.assignmentId);
      if (!mounted) return;
      setState(() {
        _submission = submission;
        _picked = null;
      });
      messenger.showSnackBar(
        const SnackBar(content: Text('Zadatak je predan.')),
      );
    } catch (e) {
      if (mounted) {
        setState(() => _submitError = userMessage(e));
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  /// Last segment of the stored `uploads/assignments/…` path (02 §4.7).
  static String submissionFileName(String? filePath) =>
      assignmentFileName(filePath);

  static bool _isImage(String? filePath) {
    if (filePath == null) return false;
    final name = submissionFileName(filePath).toLowerCase();
    return name.endsWith('.jpg') ||
        name.endsWith('.jpeg') ||
        name.endsWith('.png');
  }

  @override
  Widget build(BuildContext context) {
    final assignment = _assignment;
    return Scaffold(
      appBar: AppBar(title: const Text('Zadatak')),
      body: assignment == null
          ? AsyncStateView(
              isLoading: _loading,
              error: _error,
              onRetry: _load,
              child: const SizedBox.shrink(),
            )
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.only(bottom: 24),
                children: [
                  _header(assignment),
                  const Divider(height: 1),
                  const SectionHeader(title: 'Predaja'),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: _submissionSection(assignment),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _header(AssignmentDto assignment) {
    final pastDue = DateTime.now().isAfter(assignment.dueAt);
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            assignment.title,
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: LabeledValue(
                  label: 'Rok',
                  value: formatDateTime(assignment.dueAt),
                ),
              ),
              if (pastDue) const Chip(label: Text('Rok istekao')),
            ],
          ),
          ListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Predavanje'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => Navigator.of(context).pushNamed(
              AppRouter.lectureDetail,
              arguments: LectureDetailArgs(assignment.lectureId),
            ),
          ),
          Text(
            assignment.description,
            style: Theme.of(context).textTheme.bodyMedium,
          ),
        ],
      ),
    );
  }

  Widget _submissionSection(AssignmentDto assignment) {
    final submission = _submission;
    if (submission != null && submission.grade != null) {
      return _gradedState(submission);
    }
    if (submission != null) return _submittedState(submission);
    if (DateTime.now().isAfter(assignment.dueAt)) return _pastDueState();
    return _pickerState();
  }

  /// State A: nothing submitted, before due.
  Widget _pickerState() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        FilePickerField(
          enabled: !_submitting,
          pickOverride: widget.pickFiles,
          onChanged: (file) => setState(() {
            _picked = file;
            _submitError = null;
          }),
        ),
        const SizedBox(height: 12),
        FilledButton(
          onPressed: (_picked == null || _submitting) ? null : _submit,
          child: _submitting
              ? const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Text('Predaj zadatak'),
        ),
        if (_submitError != null) ...[
          const SizedBox(height: 8),
          ErrorBanner(message: _submitError!),
        ],
      ],
    );
  }

  /// State A′: nothing submitted, past due.
  Widget _pastDueState() {
    return const Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        BlockedReasonBanner(
          icon: Icons.info_outline,
          reason: 'Rok za predaju zadatka je istekao.',
        ),
        SizedBox(height: 8),
        FilledButton(onPressed: null, child: Text('Predaj zadatak')),
      ],
    );
  }

  /// State B: submitted, not graded.
  Widget _submittedState(AssignmentSubmissionDto submission) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _submittedHeader(submission),
        const SizedBox(height: 8),
        _fileRow(submission),
        const SizedBox(height: 8),
        const LabeledValue(label: 'Ocjena', value: '— · Još nije ocijenjeno'),
        const SizedBox(height: 8),
        const BlockedReasonBanner(
          icon: Icons.info_outline,
          reason: 'Zadatak je već predan. Ponovna predaja nije moguća.',
        ),
      ],
    );
  }

  /// State C: graded.
  Widget _gradedState(AssignmentSubmissionDto submission) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _submittedHeader(submission),
        const SizedBox(height: 8),
        _fileRow(submission),
        const SizedBox(height: 12),
        Text(
          '${submission.grade}',
          style: const TextStyle(
            fontSize: 48,
            fontWeight: FontWeight.bold,
            color: AppTheme.primary,
          ),
        ),
        Text(
          'Ocjena (od 100)',
          style: Theme.of(context).textTheme.bodyMedium,
        ),
        if (widget.courseId != null) ...[
          const SizedBox(height: 8),
          TextButton(
            onPressed: () => Navigator.of(context).pushNamed(
              AppRouter.ranking,
              arguments: RankingArgs(widget.courseId!),
            ),
            child: const Text('Rang lista kursa'),
          ),
        ],
      ],
    );
  }

  Widget _submittedHeader(AssignmentSubmissionDto submission) {
    final submittedAt = submission.submittedAt;
    return Row(
      children: [
        const Icon(Icons.check_circle_outline, color: AppTheme.success),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            submittedAt == null
                ? 'Predano'
                : 'Predano ${formatDateTime(submittedAt)}',
            style: Theme.of(context).textTheme.titleMedium,
          ),
        ),
      ],
    );
  }

  Widget _fileRow(AssignmentSubmissionDto submission) {
    final name = submissionFileName(submission.filePath);
    if (_isImage(submission.filePath)) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          networkImageOrPlaceholder(
            submission.filePath,
            context.read<ApiClient>(),
            size: 160,
            borderRadius: 8,
            placeholder: () => Container(
              width: 160,
              height: 160,
              color: AppTheme.surfaceContainer,
              child: const Icon(
                Icons.image_outlined,
                size: 32,
                color: AppTheme.textTertiary,
              ),
            ),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              const Icon(Icons.description_outlined),
              const SizedBox(width: 8),
              Expanded(child: Text(name)),
            ],
          ),
        ],
      );
    }
    return Row(
      children: [
        const Icon(Icons.description_outlined),
        const SizedBox(width: 8),
        Expanded(child: Text(name)),
      ],
    );
  }
}
