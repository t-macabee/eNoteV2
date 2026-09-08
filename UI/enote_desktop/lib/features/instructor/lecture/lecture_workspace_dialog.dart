import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/dialog_shell.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/pdf_report_button.dart';
import '../assignment/assignment_list_screen.dart';
import '../assignment/assignment_provider.dart';
import '../assignment_submission/submission_list_screen.dart';
import '../assignment_submission/submission_provider.dart';
import '../lecture_note/lecture_note_list_screen.dart';
import '../lecture_note/lecture_note_provider.dart';
import 'lecture_attendance_screen.dart';
import 'lecture_list_screen.dart';

sealed class _WorkspaceView {
  const _WorkspaceView();
}

class _LecturesView extends _WorkspaceView {
  const _LecturesView();
}

class _AttendanceView extends _WorkspaceView {
  final LectureDto lecture;
  const _AttendanceView(this.lecture);
}

class _NotesView extends _WorkspaceView {
  final LectureDto lecture;
  const _NotesView(this.lecture);
}

class _AssignmentsView extends _WorkspaceView {
  final LectureDto lecture;
  const _AssignmentsView(this.lecture);
}

class _SubmissionsView extends _WorkspaceView {
  final LectureDto lecture;
  final AssignmentDto assignment;
  const _SubmissionsView(this.lecture, this.assignment);
}

/// A wide workspace dialog for lecture management and its child views
/// (Attendance, Notes, Assignments, and Submissions).
class LectureWorkspaceDialog extends StatefulWidget {
  final ApiClient apiClient;
  final int courseId;
  final String courseName;

  const LectureWorkspaceDialog({
    super.key,
    required this.apiClient,
    required this.courseId,
    required this.courseName,
  });

  /// Opens the lecture workspace dialog. Returns `true` if any mutation
  /// occurred inside the workspace.
  static Future<bool?> show(
    BuildContext context, {
    required int courseId,
    required String courseName,
  }) {
    final apiClient = context.read<ApiClient>();
    return showDialog<bool>(
      context: context,
      builder: (_) => LectureWorkspaceDialog(
        apiClient: apiClient,
        courseId: courseId,
        courseName: courseName,
      ),
    );
  }

  @override
  State<LectureWorkspaceDialog> createState() => _LectureWorkspaceDialogState();
}

class _LectureWorkspaceDialogState extends State<LectureWorkspaceDialog> {
  final List<_WorkspaceView> _stack = [const _LecturesView()];
  bool _dirty = false;
  bool _canPop = false;

  void _markDirty() {
    _dirty = true;
  }

  void _close() {
    _canPop = true;
    Navigator.of(context).pop(_dirty);
  }

  void _popView() {
    if (_stack.length > 1) {
      setState(() => _stack.removeLast());
    } else {
      _close();
    }
  }

  String _crumbLabel(_WorkspaceView view) => switch (view) {
        _LecturesView() => 'Predavanja',
        _AttendanceView() => 'Prisustvo',
        _NotesView() => 'Bilješke',
        _AssignmentsView() => 'Zadaci',
        _SubmissionsView() => 'Predaje',
      };

  Widget? _getHeaderAction(_WorkspaceView top) => switch (top) {
        _AttendanceView(:final lecture) => PdfReportButton(
            label: 'Izvještaj',
            fileName: 'lecture-${lecture.id}-attendance.pdf',
            endpoint: 'instructor/lectures/${lecture.id}/attendance/report',
          ),
        _ => null,
      };

  Widget _buildBreadcrumbs() {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
            for (int i = 0; i < _stack.length; i++) ...[
              if (i > 0)
                const Padding(
                  padding: EdgeInsets.symmetric(horizontal: 6),
                  child: Text(
                    '›',
                    style: TextStyle(
                      color: AppTheme.textTertiary,
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              if (i < _stack.length - 1)
                InkWell(
                  borderRadius: BorderRadius.circular(4),
                  onTap: () {
                    setState(() {
                      _stack.removeRange(i + 1, _stack.length);
                    });
                  },
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 4,
                      vertical: 2,
                    ),
                    child: Text(
                      _crumbLabel(_stack[i]),
                      style: const TextStyle(
                        color: AppTheme.primary,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ),
                )
              else
                Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 4,
                    vertical: 2,
                  ),
                  child: Text(
                    _crumbLabel(_stack[i]),
                    style: const TextStyle(
                      color: AppTheme.textPrimary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
            ],
          ],
        ),
      );
    }

  Widget _buildCurrentView() {
    final top = _stack.last;
    return switch (top) {
      _LecturesView() => LectureListScreen(
          key: const ValueKey('workspace_lectures'),
          courseId: widget.courseId,
          courseName: widget.courseName,
          presentation: EntityListPresentation.embedded,
          listStyle: EntityListStyle.tiles,
          onOpenSubView: (lecture, subView) {
            setState(() {
              switch (subView) {
                case LectureSubView.attendance:
                  _stack.add(_AttendanceView(lecture));
                case LectureSubView.notes:
                  _stack.add(_NotesView(lecture));
                case LectureSubView.assignments:
                  _stack.add(_AssignmentsView(lecture));
              }
            });
          },
          onMutation: _markDirty,
        ),
      _AttendanceView(:final lecture) => LectureAttendanceView(
          key: ValueKey('workspace_attendance_${lecture.id}'),
          lectureId: lecture.id,
          onMutation: _markDirty,
        ),
      _NotesView(:final lecture) => ChangeNotifierProvider<LectureNoteProvider>(
          key: ValueKey('workspace_notes_${lecture.id}'),
          create: (_) => LectureNoteProvider(
            apiClient: widget.apiClient,
            lectureId: lecture.id,
          ),
          child: LectureNoteListScreen(
            lectureId: lecture.id,
            lectureName: lecture.name,
            presentation: EntityListPresentation.embedded,
            onMutation: _markDirty,
          ),
        ),
      _AssignmentsView(:final lecture) =>
        ChangeNotifierProvider<AssignmentProvider>(
          key: ValueKey('workspace_assignments_${lecture.id}'),
          create: (_) => AssignmentProvider(
            apiClient: widget.apiClient,
            lectureId: lecture.id,
          ),
          child: AssignmentListScreen(
            lectureId: lecture.id,
            lectureName: lecture.name,
            presentation: EntityListPresentation.embedded,
            onOpenSubmissions: (assignment) {
              setState(() {
                _stack.add(_SubmissionsView(lecture, assignment));
              });
            },
            onMutation: _markDirty,
          ),
        ),
      _SubmissionsView(:final lecture, :final assignment) =>
        ChangeNotifierProvider<SubmissionProvider>(
          key: ValueKey('workspace_submissions_${assignment.id}'),
          create: (_) => SubmissionProvider(
            apiClient: widget.apiClient,
            lectureId: lecture.id,
            assignmentId: assignment.id,
          ),
          child: SubmissionListScreen(
            lectureId: lecture.id,
            assignmentId: assignment.id,
            assignmentTitle: assignment.title,
            presentation: EntityListPresentation.embedded,
            onMutation: _markDirty,
          ),
        ),
    };
  }

  Widget _buildHeader(_WorkspaceView top) {
    final headerAction = _getHeaderAction(top);
    return DialogShellHeader(
      leading: _stack.length > 1
          ? IconButton(
              icon: const Icon(Icons.arrow_back),
              tooltip: 'Nazad',
              onPressed: _popView,
            )
          : null,
      label: _buildBreadcrumbs(),
      action: headerAction,
      onClose: _close,
    );
  }

  @override
  Widget build(BuildContext context) {
    final top = _stack.last;

    return PopScope(
      canPop: _canPop,
      onPopInvokedWithResult: (didPop, result) {
        if (didPop) return;
        if (_stack.length > 1) {
          _popView();
        } else {
          _canPop = true;
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (mounted) {
              Navigator.of(context).pop(_dirty);
            }
          });
        }
      },
      child: DialogShell(
        width: DialogShellWidth.lg,
        header: _buildHeader(top),
        body: _buildCurrentView(),
      ),
    );
  }
}
