import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/dialog_shell.dart';
import '../../../widgets/entity_filter_dropdown.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/reason_prompt_dialog.dart';
import 'course_provider.dart';

Color _enrollmentStatusColor(EnrollmentStatus status) => switch (status) {
  EnrollmentStatus.pending => Colors.orange,
  EnrollmentStatus.active => Colors.green,
  EnrollmentStatus.completed => Colors.blue,
  EnrollmentStatus.rejected => Colors.red,
  EnrollmentStatus.canceled => Colors.grey,
};

class CourseEnrollmentsDialog extends StatefulWidget {
  final int courseId;
  final String courseName;

  const CourseEnrollmentsDialog({
    super.key,
    required this.courseId,
    required this.courseName,
  });

  static Future<bool?> show(
    BuildContext context, {
    required int courseId,
    required String courseName,
  }) {
    return showDialog<bool>(
      context: context,
      builder: (_) => CourseEnrollmentsDialog(
        courseId: courseId,
        courseName: courseName,
      ),
    );
  }

  @override
  State<CourseEnrollmentsDialog> createState() =>
      _CourseEnrollmentsDialogState();
}

class _CourseEnrollmentsDialogState extends State<CourseEnrollmentsDialog> {
  final _listKey = GlobalKey<EntityListScreenState<CourseEnrollmentDto>>();
  EnrollmentStatus? _selectedStatus = EnrollmentStatus.pending;
  bool _changed = false;
  bool _busy = false;

  Future<void> _approve(CourseEnrollmentDto item) async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Odobri upis',
      message: 'Odobriti upis za studenta ${item.studentName}?',
    );
    if (confirmed != true || !mounted) return;
    await _run(
      () => context.read<CourseProvider>().approveEnrollment(
        widget.courseId,
        item.id,
      ),
      'Upis odobren.',
    );
  }

  Future<void> _reject(CourseEnrollmentDto item) async {
    final reason = await promptForReason(context, title: 'Odbij upis');
    if (reason == null || !mounted) return;
    await _run(
      () => context.read<CourseProvider>().rejectEnrollment(
        widget.courseId,
        item.id,
        reason,
      ),
      'Upis odbijen.',
    );
  }

  Future<void> _complete(CourseEnrollmentDto item) async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Označi kao položen',
      message:
          'Označiti da je student ${item.studentName} položio kurs? '
          'Student time gubi pristup sadržaju kursa.',
    );
    if (confirmed != true || !mounted) return;
    await _run(
      () => context.read<CourseProvider>().completeEnrollment(
        widget.courseId,
        item.id,
      ),
      'Upis označen kao položen.',
    );
  }

  Future<void> _run(Future<void> Function() action, String message) async {
    setState(() => _busy = true);
    try {
      await action();
      if (!mounted) return;
      _changed = true;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(message)),
      );
      _listKey.currentState?.refresh();
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  Widget _buildFilterBar() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          EntityFilterDropdown<EnrollmentStatus?>(
            label: 'Status',
            width: 220,
            outlined: true,
            value: _selectedStatus,
            items: [
              const DropdownMenuItem(value: null, child: Text('Svi statusi')),
              for (final status in EnrollmentStatus.values)
                DropdownMenuItem(
                  value: status,
                  child: Text(enrollmentStatusLabel(status)),
                ),
            ],
            onChanged: (value) {
              setState(() => _selectedStatus = value);
              _listKey.currentState?.refresh();
            },
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return DialogShell(
      width: DialogShellWidth.lg,
      header: DialogShellHeader(
        label: Text(
          'Upisi — ${widget.courseName}',
          style: const TextStyle(
            color: AppTheme.textPrimary,
            fontWeight: FontWeight.w600,
            fontSize: 16,
          ),
        ),
        onClose: () => Navigator.of(context).pop(_changed),
      ),
      body: EntityListScreen<CourseEnrollmentDto>(
        key: _listKey,
        config: EntityListConfig<CourseEnrollmentDto>(
          presentation: EntityListPresentation.embedded,
          listStyle: EntityListStyle.tiles,
          showSearchBar: false,
          showAddButton: false,
          onEdit: null,
          onDelete: null,
          columns: [
            ColumnSpec<CourseEnrollmentDto>(
              label: 'Student',
              value: (item) => item.studentName,
            ),
            ColumnSpec<CourseEnrollmentDto>(
              label: 'Status',
              value: (item) => enrollmentStatusLabel(item.enrollmentStatus),
              cellBuilder: (context, item) => Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 8,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: _enrollmentStatusColor(item.enrollmentStatus)
                      .withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  enrollmentStatusLabel(item.enrollmentStatus),
                  style: TextStyle(
                    color: _enrollmentStatusColor(item.enrollmentStatus),
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
            ),
            ColumnSpec<CourseEnrollmentDto>(
              label: 'Plaćeno do',
              value: (item) => formatDateNullable(item.paidUntil),
            ),
            ColumnSpec<CourseEnrollmentDto>(
              label: 'Odlučeno',
              value: (item) => formatDateNullable(item.decidedAt),
            ),
            ColumnSpec<CourseEnrollmentDto>(
              label: 'Razlog',
              value: (item) => item.decisionNote ?? '-',
            ),
          ],
          filterBar: _buildFilterBar(),
          extraActions: (context, item) => [
            Tooltip(
              message: item.enrollmentStatus == EnrollmentStatus.pending
                  ? 'Odobri'
                  : 'Samo zahtjev na čekanju se može odobriti.',
              child: IconButton(
                icon: const Icon(Icons.check_circle_outline, size: 18),
                onPressed:
                    item.enrollmentStatus == EnrollmentStatus.pending && !_busy
                    ? () => _approve(item)
                    : null,
              ),
            ),
            Tooltip(
              message: item.enrollmentStatus == EnrollmentStatus.pending
                  ? 'Odbij'
                  : 'Samo zahtjev na čekanju se može odbiti.',
              child: IconButton(
                color: Colors.red,
                icon: const Icon(Icons.cancel_outlined, size: 18),
                onPressed:
                    item.enrollmentStatus == EnrollmentStatus.pending && !_busy
                    ? () => _reject(item)
                    : null,
              ),
            ),
            Tooltip(
              message: item.enrollmentStatus == EnrollmentStatus.active
                  ? 'Položio'
                  : 'Samo aktivan upis može biti označen kao položen.',
              child: IconButton(
                icon: const Icon(Icons.school_outlined, size: 18),
                onPressed:
                    item.enrollmentStatus == EnrollmentStatus.active && !_busy
                    ? () => _complete(item)
                    : null,
              ),
            ),
          ],
          fetcher: (page, pageSize, search) =>
              context.read<CourseProvider>().getEnrollments(
                widget.courseId,
                params: pagedQuery(
                  page,
                  pageSize,
                  search,
                  filters: {
                    if (_selectedStatus != null)
                      'enrollmentStatus': _selectedStatus!.toJson(),
                  },
                ),
              ),
        ),
      ),
    );
  }
}
