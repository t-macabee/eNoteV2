import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_filter_dropdown.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/status_chip.dart';
import 'course_provider.dart';

class CourseEnrollmentsView extends StatefulWidget {
  final int courseId;
  final VoidCallback onMutation;

  const CourseEnrollmentsView({
    super.key,
    required this.courseId,
    required this.onMutation,
  });

  @override
  State<CourseEnrollmentsView> createState() => _CourseEnrollmentsViewState();
}

class _CourseEnrollmentsViewState extends State<CourseEnrollmentsView> {
  final _listKey = GlobalKey<EntityListScreenState<CourseEnrollmentDto>>();
  EnrollmentStatus? _selectedStatus = EnrollmentStatus.pending;
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
      widget.onMutation();
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(message)));
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
    return EntityListScreen<CourseEnrollmentDto>(
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
            cellBuilder: (context, item) {
              return StatusChip(
                label: enrollmentStatusLabel(item.enrollmentStatus),
                tone: enrollmentStatusTone(item.enrollmentStatus),
              );
            },
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
    );
  }
}
