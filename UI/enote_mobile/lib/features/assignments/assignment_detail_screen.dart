import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/detail_header.dart';
import '../../widgets/detail_section.dart';
import '../../widgets/form_submit_state.dart';
import '../../widgets/labeled_value.dart';
import '../../widgets/submit_button.dart';
import 'assignment_labels.dart';
import 'assignment_provider.dart';
import 'file_picker_field.dart';

const double _previewSize = 160;

class AssignmentDetailScreen extends StatefulWidget {
  final int assignmentId;
  final int? courseId;

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

class _AssignmentDetailScreenState extends State<AssignmentDetailScreen>
    with FormSubmitState<AssignmentDetailScreen> {
  AssignmentDto? _assignment;
  AssignmentSubmissionDto? _submission;
  Object? _error;
  bool _loading = true;
  PickedAssignmentFile? _picked;

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
    if (picked == null || isSubmitting) return;
    final confirmed = await confirmDialog(
      context: context,
      title: 'Predaja zadatka',
      message: 'Predati "${picked.fileName}"? Predaju nije moguće izmijeniti.',
    );
    if (confirmed != true || !mounted) return;
    final provider = context.read<AssignmentProvider>();
    final messenger = ScaffoldMessenger.of(context);
    await submit(() async {
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
    });
  }

  static bool _isImage(String? filePath) {
    if (filePath == null) return false;
    final name = assignmentFileName(filePath).toLowerCase();
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
                padding: const EdgeInsets.only(bottom: AppSpacing.lg),
                children: [
                  _header(assignment),
                  DetailSection(title: 'Opis', child: _description(assignment)),
                  DetailSection(
                    title: 'Predaja',
                    child: _submissionSection(assignment),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _header(AssignmentDto assignment) {
    final status = assignmentStatus(
      dueAt: assignment.dueAt,
      submittedAt: _submission?.submittedAt,
      grade: _submission?.grade,
      now: DateTime.now(),
    );
    return DetailHeader(
      title: assignment.title,
      subtitle: 'Rok: ${formatDateTime(assignment.dueAt)}',
      chips: [StatusChip(label: status.label, tone: status.tone)],
    );
  }

  Widget _description(AssignmentDto assignment) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          assignment.description,
          style: Theme.of(context).textTheme.bodyMedium,
        ),
        ListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Predavanje'),
          trailing: const Icon(Icons.chevron_right),
          onTap: () => Navigator.of(context).pushNamed(
            AppRoutes.lectureDetail,
            arguments: LectureDetailArgs(assignment.lectureId),
          ),
        ),
      ],
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

  Widget _pickerState() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        FilePickerField(
          enabled: !isSubmitting,
          pickOverride: widget.pickFiles,
          onChanged: (file) => setState(() {
            _picked = file;
            submitError = null;
          }),
        ),
        const SizedBox(height: AppSpacing.md),
        SubmitButton(
          label: 'Predaj zadatak',
          onPressed: _picked == null ? null : _submit,
          isBusy: isSubmitting,
        ),
        if (submitError != null) ...[
          const SizedBox(height: AppSpacing.sm),
          ErrorBanner(message: submitError!),
        ],
      ],
    );
  }

  Widget _pastDueState() {
    return const Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        StatusBanner(
          icon: Icons.info_outline,
          message: 'Rok za predaju zadatka je istekao.',
          tone: StatusTone.neutral,
        ),
        SizedBox(height: AppSpacing.sm),
        FilledButton(onPressed: null, child: Text('Predaj zadatak')),
      ],
    );
  }

  Widget _submittedState(AssignmentSubmissionDto submission) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _submittedHeader(submission),
        const SizedBox(height: AppSpacing.sm),
        _fileRow(submission),
        const SizedBox(height: AppSpacing.sm),
        const LabeledValue(label: 'Ocjena', value: '— · Još nije ocijenjeno'),
        const SizedBox(height: AppSpacing.sm),
        const StatusBanner(
          icon: Icons.info_outline,
          message: 'Zadatak je već predan. Ponovna predaja nije moguća.',
          tone: StatusTone.info,
        ),
      ],
    );
  }

  Widget _gradedState(AssignmentSubmissionDto submission) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _submittedHeader(submission),
        const SizedBox(height: AppSpacing.sm),
        _fileRow(submission),
        const SizedBox(height: AppSpacing.md),
        Text(
          '${submission.grade}',
          style: Theme.of(context).textTheme.displaySmall
              ?.copyWith(color: AppTheme.primary),
        ),
        Text('Ocjena (od 100)', style: Theme.of(context).textTheme.bodyMedium),
        if (submission.feedback != null) ...[
          const SizedBox(height: AppSpacing.md),
          Text(
            'Komentar instruktora',
            style: Theme.of(context).textTheme.titleSmall,
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(submission.feedback!),
        ],
        if (widget.courseId != null) ...[
          const SizedBox(height: AppSpacing.sm),
          TextButton(
            onPressed: () => Navigator.of(context).pushNamed(
              AppRoutes.ranking,
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
        const SizedBox(width: AppSpacing.sm),
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
    final name = assignmentFileName(submission.filePath);
    final fileName = Row(
      children: [
        const Icon(Icons.description_outlined),
        const SizedBox(width: AppSpacing.sm),
        Expanded(child: Text(name)),
      ],
    );
    if (!_isImage(submission.filePath)) return fileName;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        networkImageOrPlaceholder(
          submission.filePath,
          context.read<ApiClient>(),
          size: _previewSize,
          borderRadius: AppRadius.md,
          placeholder: () =>
              const ImagePlaceholder(width: _previewSize, height: _previewSize),
        ),
        const SizedBox(height: AppSpacing.sm),
        fileName,
      ],
    );
  }
}
