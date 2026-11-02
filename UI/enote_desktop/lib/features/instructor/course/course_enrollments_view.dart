import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/workflow_actions.dart';
import '../enrollment/enrollment_actions.dart';
import '../enrollment/instructor_enrollment_provider.dart';

class CourseEnrollmentsView extends StatefulWidget {
  final int courseId;
  final int? pendingCount;
  final VoidCallback onMutation;

  const CourseEnrollmentsView({
    super.key,
    required this.courseId,
    required this.pendingCount,
    required this.onMutation,
  });

  @override
  State<CourseEnrollmentsView> createState() => _CourseEnrollmentsViewState();
}

class _CourseEnrollmentsViewState extends State<CourseEnrollmentsView> {
  final _listKey = GlobalKey<EntityListScreenState<CourseEnrollmentDto>>();
  EnrollmentStatus? _selectedStatus = EnrollmentStatus.pending;

  void _onStatusChanged(EnrollmentStatus? value) {
    setState(() => _selectedStatus = value);
    _listKey.currentState?.refresh(resetPage: true);
  }

  Future<void> _run(
    CourseEnrollmentDto item,
    EnrollmentAction action,
    String? note,
  ) async {
    final provider = context.read<InstructorEnrollmentProvider>();
    await runEnrollmentAction(provider, item, action, note);
    if (!mounted) return;
    widget.onMutation();
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(enrollmentSuccessMessage(action))));
    _listKey.currentState?.refresh();
  }

  Widget _buildStatusTabs() {
    return StatusFilterTabs<EnrollmentStatus?>(
      selected: _selectedStatus,
      options: enrollmentStatusFilterOptions(pendingCount: widget.pendingCount),
      onChanged: _onStatusChanged,
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
        statusTabs: _buildStatusTabs(),
        extraActions: (context, item) => [
          WorkflowActionButtons<EnrollmentAction>(
            actions: enrollmentActions(item),
            compact: true,
            onRun: (trigger, {note}) => _run(item, trigger, note),
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<InstructorEnrollmentProvider>().getForCourse(
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
