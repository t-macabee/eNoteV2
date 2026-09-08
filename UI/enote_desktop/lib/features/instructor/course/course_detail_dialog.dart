import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/detail_row.dart';
import '../../../widgets/dialog_shell.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/pdf_report_button.dart';
import '../../shared/announcement/announcement_list_screen.dart';
import '../../shared/announcement/announcement_provider.dart';
import '../lecture/lecture_workspace_dialog.dart';
import '../ranking/ranking_screen.dart';
import 'course_form_screen.dart';
import 'course_provider.dart';

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

class _CourseDetailDialogState extends State<CourseDetailDialog> {
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
    setState(() => _isPublishing = true);
    try {
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
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
        setState(() {});
      }
    } finally {
      if (mounted) {
        setState(() => _isPublishing = false);
      }
    }
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

    setState(() => _isDeleting = true);
    try {
      await context.read<CourseProvider>().remove(_course.id);
      if (mounted) {
        _popDialog(true);
      }
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _isDeleting = false);
      }
    }
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
    final apiClient = context.read<ApiClient>();
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => DialogShell(
        width: DialogShellWidth.lg,
        header: DialogShellHeader(
          label: const Text(
            'Objave',
            style: TextStyle(
              color: AppTheme.textPrimary,
              fontWeight: FontWeight.w600,
              fontSize: 16,
            ),
          ),
          onClose: () => Navigator.of(dialogContext).pop(),
        ),
        body: ChangeNotifierProvider<AnnouncementProvider>(
          create: (_) => AnnouncementProvider(
            apiClient: apiClient,
            courseId: _course.id,
          ),
          builder: (ctx, _) => AnnouncementListScreen(
            provider: ctx.read<AnnouncementProvider>(),
            presentation: EntityListPresentation.embedded,
          ),
        ),
      ),
    );
    if (mounted) {
      _refreshCourse();
    }
  }

  Future<void> _openRanking() async {
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => DialogShell(
        width: DialogShellWidth.lg,
        header: DialogShellHeader(
          label: const Text(
            'Rangiranje',
            style: TextStyle(
              color: AppTheme.textPrimary,
              fontWeight: FontWeight.w600,
              fontSize: 16,
            ),
          ),
          action: PdfReportButton(
            label: 'Izvještaj',
            fileName: 'course-${_course.id}-ranking.pdf',
            endpoint: 'instructor/courses/${_course.id}/ranking/report',
          ),
          onClose: () => Navigator.of(dialogContext).pop(),
        ),
        body: RankingView(courseId: _course.id),
      ),
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
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 12, 24, 0),
              child: Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  OutlinedButton.icon(
                    icon: const Icon(Icons.event_note, size: 18),
                    label: const Text('Predavanja'),
                    onPressed: _openLectures,
                  ),
                  OutlinedButton.icon(
                    icon: const Icon(Icons.campaign, size: 18),
                    label: const Text('Objave'),
                    onPressed: _openAnnouncements,
                  ),
                  OutlinedButton.icon(
                    icon: const Icon(Icons.leaderboard, size: 18),
                    label: const Text('Rangiranje'),
                    onPressed: _openRanking,
                  ),
                ],
              ),
            ),
            Flexible(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(24, 16, 24, 16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    DetailRow(
                      icon: Icons.description_outlined,
                      label: 'Opis',
                      value: (_course.description != null &&
                              _course.description!.isNotEmpty)
                          ? _course.description!
                          : '-',
                    ),
                    DetailRow(
                      icon: Icons.payments_outlined,
                      label: 'Cijena',
                      value: _course.price.toStringAsFixed(2),
                    ),
                    DetailRow(
                      icon: Icons.event_outlined,
                      label: 'Datum početka',
                      value: formatDateNullable(_course.startDate),
                    ),
                    DetailRow(
                      icon: Icons.event_available_outlined,
                      label: 'Datum završetka',
                      value: formatDateNullable(_course.endDate),
                    ),
                    DetailRow(
                      icon: Icons.groups_outlined,
                      label: 'Broj upisanih',
                      value: _course.enrolledCount.toString(),
                    ),
                  ],
                ),
              ),
            ),
            const Divider(height: 1),
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 12, 24, 16),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  OutlinedButton.icon(
                    onPressed:
                        _isDeleting || _isPublishing ? null : _delete,
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppTheme.error,
                      side: const BorderSide(color: AppTheme.error),
                    ),
                    icon: _isDeleting
                        ? const SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: AppTheme.error,
                            ),
                          )
                        : const Icon(Icons.delete_outline, size: 18),
                    label: const Text('Obriši'),
                  ),
                  const Spacer(),
                  const Text(
                    'Objavljen',
                    style: TextStyle(color: AppTheme.textSecondary),
                  ),
                  const SizedBox(width: 8),
                  if (_isPublishing)
                    const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  else
                    Switch(
                      value: _course.isPublished,
                      onChanged: _isDeleting ? null : _togglePublish,
                    ),
                  const SizedBox(width: 8),
                  FilledButton.icon(
                    onPressed:
                        _isDeleting || _isPublishing ? null : _openEdit,
                    icon: const Icon(Icons.edit_outlined, size: 18),
                    label: const Text('Uredi'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
