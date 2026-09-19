import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/dialog_shell.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/form_submit_state.dart';
import '../lecture/lecture_workspace_dialog.dart';
import '../ranking/ranking_provider.dart';
import 'course_announcements_dialog.dart';
import 'course_detail_sections.dart';
import 'course_form_screen.dart';
import 'course_provider.dart';
import 'course_ranking_dialog.dart';

class CourseDetailDialog extends StatefulWidget {
  final CourseDto course;

  const CourseDetailDialog({
    super.key,
    required this.course,
  });

  static Future<bool?> show(BuildContext context, CourseDto course) {
    return showDialog<bool>(
      context: context,
      builder: (_) => CourseDetailDialog(course: course),
    );
  }

  @override
  State<CourseDetailDialog> createState() => _CourseDetailDialogState();
}

class _CourseDetailDialogState extends State<CourseDetailDialog>
    with FormSubmitState<CourseDetailDialog> {
  late CourseDto _course;
  bool _isPublishing = false;
  bool _isDeleting = false;
  bool _dirty = false;
  bool _canPop = false;
  bool _popped = false;

  @override
  void initState() {
    super.initState();
    _course = widget.course;
  }

  void _popDialog(bool result) {
    if (_popped) return;
    _popped = true;
    _canPop = true;
    Navigator.of(context).pop(result);
  }

  void _close() {
    _popDialog(_dirty);
  }

  Future<void> _togglePublish(bool newValue) async {
    if (_course.isPublished && !newValue) {
      final confirmed = await confirmDialog(
        context: context,
        title: 'Potvrdite povlačenje kursa',
        message:
            'Da li ste sigurni da želite da povučete ovaj kurs? Kurs nestaje iz kataloga i novi studenti ga ne mogu upisati. Postojeći studenti zadržavaju pristup.',
      );
      if (confirmed != true || !mounted) return;
    }
    await submitWith((busy) => _isPublishing = busy, () async {
      final request = CourseRequest(
        name: _course.name,
        description: _course.description,
        price: _course.price,
        startDate: _course.startDate,
        endDate: _course.endDate,
        isPublished: newValue,
      );
      final updated = await context.read<CourseProvider>().update(
            _course.id,
            request.toJson(),
          );
      if (mounted) {
        setState(() {
          _course = updated;
          _dirty = true;
        });
      }
    });
  }

  Future<void> _openEdit() async {
    final updated = await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => CourseFormScreen(
        existing: _course,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    if (updated == true && mounted) {
      try {
        final fresh = await context.read<CourseProvider>().getById(_course.id);
        if (mounted) {
          setState(() {
            _course = fresh;
            _dirty = true;
          });
        }
      } catch (e) {
        if (mounted) {
          ErrorBanner.show(context, message: userMessage(e));
        }
      }
    }
  }

  Future<void> _delete() async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrdite brisanje',
      message:
          'Da li ste sigurni da želite da obrišete ovaj kurs? Brisanjem kursa deaktiviraće se i njegova predavanja.',
    );
    if (confirmed != true || !mounted) return;

    await submitWith((busy) => _isDeleting = busy, () async {
      await context.read<CourseProvider>().remove(_course.id);
      if (mounted) {
        _popDialog(true);
      }
    });
  }

  Future<void> _openLectures() async {
    final changed = await LectureWorkspaceDialog.show(
      context,
      courseId: _course.id,
      courseName: _course.name,
    );
    if (changed == true && mounted) {
      _refreshCourse();
    }
  }

  Future<void> _openAnnouncements() async {
    await CourseAnnouncementsDialog.show(
      context,
      courseId: _course.id,
      apiClient: context.read<ApiClient>(),
    );
    if (mounted) {
      _refreshCourse();
    }
  }

  Future<void> _openRanking() async {
    await CourseRankingDialog.show(
      context,
      courseId: _course.id,
      reportEndpoint:
          context.read<RankingProvider>().reportEndpoint(_course.id),
    );
  }

  Future<void> _refreshCourse() async {
    try {
      final fresh = await context.read<CourseProvider>().getById(_course.id);
      if (mounted) {
        setState(() {
          _course = fresh;
          _dirty = true;
        });
      }
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: _canPop,
      onPopInvokedWithResult: (didPop, result) {
        if (didPop) return;
        _canPop = true;
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted) {
            _popDialog(_dirty);
          }
        });
      },
      child: DialogShell(
        title: _course.name,
        width: DialogShellWidth.md,
        onClose: _close,
        body: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            CourseDetailActionBar(
              onLectures: _openLectures,
              onAnnouncements: _openAnnouncements,
              onRanking: _openRanking,
            ),
            Flexible(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(24, 16, 24, 16),
                child: CourseDetailRows(course: _course),
              ),
            ),
            const Divider(height: 1),
            CourseDetailFooter(
              isPublished: _course.isPublished,
              isDeleting: _isDeleting,
              isPublishing: _isPublishing,
              onDelete: _delete,
              onPublishChanged: _togglePublish,
              onEdit: _openEdit,
            ),
          ],
        ),
      ),
    );
  }
}
