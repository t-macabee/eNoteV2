import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/dashboard_count.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/status_chip.dart';
import '../../../widgets/status_filter_tabs.dart';
import '../../../widgets/workflow_actions.dart';
import '../course/course_detail_screen.dart';
import '../course/course_provider.dart';
import 'enrollment_actions.dart';
import 'instructor_enrollment_provider.dart';

class InstructorEnrollmentListScreen extends StatefulWidget {
  const InstructorEnrollmentListScreen({super.key});

  @override
  State<InstructorEnrollmentListScreen> createState() =>
      _InstructorEnrollmentListScreenState();
}

class _InstructorEnrollmentListScreenState
    extends State<InstructorEnrollmentListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<CourseEnrollmentDto>>();
  final _pendingCount = DashboardCount();
  EnrollmentStatus? _selectedStatus = EnrollmentStatus.pending;

  @override
  void initState() {
    super.initState();
    _loadPendingCount();
  }

  @override
  void dispose() {
    _pendingCount.dispose();
    super.dispose();
  }

  Future<void> _loadPendingCount() async {
    if (!mounted) return;
    await _pendingCount.load(
      () => context.read<InstructorEnrollmentProvider>().countPending(),
    );
  }

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
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(enrollmentSuccessMessage(action))));
    _listKey.currentState?.refresh();
    await _loadPendingCount();
  }

  Future<void> _openCourse(CourseEnrollmentDto item) async {
    try {
      final course = await context.read<CourseProvider>().getById(
        item.courseId,
      );
      if (!mounted) return;
      await CourseDetailScreen.open(
        context,
        course,
        initialTab: CourseDetailTab.enrollments,
      );
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
      return;
    }
    if (!mounted) return;
    _listKey.currentState?.refresh();
    await _loadPendingCount();
  }

  Widget _buildStatusTabs() {
    return ListenableBuilder(
      listenable: _pendingCount,
      builder: (context, _) => StatusFilterTabs<EnrollmentStatus?>(
        selected: _selectedStatus,
        options: enrollmentStatusFilterOptions(
          pendingCount: _pendingCount.value,
        ),
        onChanged: _onStatusChanged,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<CourseEnrollmentDto>(
      key: _listKey,
      config: EntityListConfig<CourseEnrollmentDto>(
        presentation: EntityListPresentation.embedded,
        columns: [
          ColumnSpec<CourseEnrollmentDto>(
            label: 'Student',
            value: (item) => item.studentName,
            maxWidth: 180,
          ),
          ColumnSpec<CourseEnrollmentDto>(
            label: 'Kurs',
            value: (item) => item.courseName,
            maxWidth: 200,
          ),
          ColumnSpec<CourseEnrollmentDto>(
            label: 'Status',
            value: (item) => enrollmentStatusLabel(item.enrollmentStatus),
            cellBuilder: (context, item) => StatusChip(
              label: enrollmentStatusLabel(item.enrollmentStatus),
              tone: enrollmentStatusTone(item.enrollmentStatus),
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
        ],
        showSearchBar: false,
        showAddButton: false,
        showDeleteConfirmation: false,
        statusTabs: _buildStatusTabs(),
        fetcher: (page, pageSize, search) =>
            context.read<InstructorEnrollmentProvider>().search(
              pagedQuery(
                page,
                pageSize,
                search,
                filters: {
                  if (_selectedStatus != null)
                    'enrollmentStatus': _selectedStatus!.toJson(),
                },
              ),
            ),
        onRowTap: (context, item) => _openCourse(item),
        extraActions: (context, item) {
          final actions = enrollmentActions(item);
          if (actions.isEmpty) return const <Widget>[];
          return [
            WorkflowActionButtons<EnrollmentAction>(
              actions: actions,
              compact: true,
              onRun: (trigger, {note}) => _run(item, trigger, note),
            ),
          ];
        },
      ),
    );
  }
}
