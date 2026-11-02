import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/status_chip.dart';
import '../../../widgets/workflow_actions.dart';
import 'lecture_provider.dart';

class LectureAttendanceView extends StatefulWidget {
  final int lectureId;
  final VoidCallback? onMutation;

  const LectureAttendanceView({
    super.key,
    required this.lectureId,
    this.onMutation,
  });

  @override
  State<LectureAttendanceView> createState() => _LectureAttendanceViewState();
}

class _LectureAttendanceViewState extends State<LectureAttendanceView> {
  final _listKey = GlobalKey<EntityListScreenState<AttendanceDto>>();

  Future<void> _markAttendance(
    AttendanceDto item,
    AttendanceStatus newStatus,
  ) async {
    await context.read<LectureProvider>().markAttendance(
      widget.lectureId,
      MarkAttendanceRequest(
        studentId: item.studentId,
        attendanceStatus: newStatus,
      ),
    );
    if (!mounted) return;
    widget.onMutation?.call();
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          'Prisustvo ažurirano: ${attendanceStatusLabel(newStatus)}',
        ),
      ),
    );
    _listKey.currentState?.refresh();
  }

  List<WorkflowAction<AttendanceStatus>> _attendanceActions(
    AttendanceDto item,
  ) => [
    if (item.attendanceStatus != AttendanceStatus.present)
      const WorkflowAction(
        trigger: AttendanceStatus.present,
        label: 'Prisutan',
        icon: Icons.check_circle_outline,
        tone: StatusTone.success,
      ),
    if (item.attendanceStatus != AttendanceStatus.absent)
      const WorkflowAction(
        trigger: AttendanceStatus.absent,
        label: 'Odsutan',
        icon: Icons.cancel_outlined,
        tone: StatusTone.danger,
      ),
  ];

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<AttendanceDto>(
      key: _listKey,
      config: EntityListConfig<AttendanceDto>(
        presentation: EntityListPresentation.embedded,
        listStyle: EntityListStyle.tiles,
        showSearchBar: false,
        showAddButton: false,
        columns: [
          ColumnSpec<AttendanceDto>(
            label: 'Student',
            value: (item) => item.studentName,
          ),
          ColumnSpec<AttendanceDto>(
            label: 'Status',
            value: (item) => attendanceStatusLabel(item.attendanceStatus),
            cellBuilder: (context, item) => StatusChip(
              label: attendanceStatusLabel(item.attendanceStatus),
              tone: attendanceStatusTone(item.attendanceStatus),
            ),
          ),
        ],
        extraActions: (context, item) => [
          WorkflowActionButtons<AttendanceStatus>(
            actions: _attendanceActions(item),
            compact: true,
            onRun: (trigger, {note}) => _markAttendance(item, trigger),
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<LectureProvider>().getAttendance(
              widget.lectureId,
              params: pagedQuery(page, pageSize, search),
            ),
      ),
    );
  }
}
