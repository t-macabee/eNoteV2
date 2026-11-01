import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/entity_detail_page.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_form_sections.dart';
import '../../../widgets/form_submit_state.dart';
import '../lecture/lecture_workspace_view.dart';
import '../ranking/ranking_provider.dart';
import 'course_announcements_view.dart';
import 'course_detail_sections.dart';
import 'course_enrollments_view.dart';
import 'course_form_screen.dart';
import 'course_provider.dart';
import 'course_ranking_view.dart';

class CourseDetailScreen extends StatefulWidget {
  final CourseDto course;

  const CourseDetailScreen({super.key, required this.course});

  static Future<bool?> open(BuildContext context, CourseDto course) {
    return Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => CourseDetailScreen(course: course)),
    );
  }

  @override
  State<CourseDetailScreen> createState() => _CourseDetailScreenState();
}

class _CourseDetailScreenState extends State<CourseDetailScreen>
    with FormSubmitState<CourseDetailScreen> {
  late CourseDto _course;
  bool _isPublishing = false;
  bool _isDeleting = false;
  bool _dirty = false;
  bool _popped = false;

  @override
  void initState() {
    super.initState();
    _course = widget.course;
  }

  void _pop(bool result) {
    if (_popped) return;
    _popped = true;
    Navigator.of(context).pop(result);
  }

  void _close() {
    _pop(_dirty);
  }

  Future<void> _togglePublish(bool newValue) async {
    if (_course.isPublished && !newValue) {
      final confirmed = await confirmDialog(
        context: context,
        title: 'Potvrdite povlačenje kursa',
        message: 'Da li ste sigurni da želite da povučete ovaj kurs? Kurs nestaje iz kataloga i novi studenti ga ne mogu upisati. Postojeći studenti zadržavaju pristup.',
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
          'Da li ste sigurni da želite da obrišete kurs "${_course.name}"? Brisanjem kursa deaktiviraće se i njegova predavanja.',
    );
    if (confirmed != true || !mounted) return;

    await submitWith((busy) => _isDeleting = busy, () async {
      await context.read<CourseProvider>().remove(_course.id);
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Kurs je obrisan.')));
        _pop(true);
      }
    });
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
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
    }
  }

  List<Widget> _buildActions() {
    return [
      TextButton.icon(
        onPressed: _isDeleting || _isPublishing ? null : _openEdit,
        icon: const Icon(Icons.edit_outlined),
        label: const Text('Uredi'),
      ),
      Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Text(
            'Objavljen',
            style: TextStyle(color: AppTheme.textSecondary),
          ),
          const SizedBox(width: AppSpacing.sm),
          if (_isPublishing)
            const SizedBox(
              width: AppSize.spinnerMd,
              height: AppSize.spinnerMd,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          else
            Switch(
              value: _course.isPublished,
              onChanged: _isDeleting ? null : _togglePublish,
            ),
        ],
      ),
      TextButton.icon(
        onPressed: _isDeleting || _isPublishing ? null : _delete,
        style: TextButton.styleFrom(foregroundColor: AppTheme.error),
        icon: _isDeleting
            ? const SizedBox(
                width: AppSize.spinnerSm,
                height: AppSize.spinnerSm,
                child: CircularProgressIndicator(
                  strokeWidth: 2,
                  color: AppTheme.error,
                ),
              )
            : const Icon(Icons.delete_outline),
        label: const Text('Obriši'),
      ),
      const SizedBox(width: AppSpacing.sm),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, result) {
        if (didPop) return;
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted) {
            _pop(_dirty);
          }
        });
      },
      child: EntityDetailPage(
        title: _course.name,
        onClose: _close,
        actions: _buildActions(),
        tabs: [
          EntityDetailTab(
            label: 'Pregled',
            icon: Icons.class_,
            builder: (_) => _CourseOverview(course: _course),
          ),
          EntityDetailTab(
            label: 'Predavanja',
            icon: Icons.event_note,
            builder: (_) => LectureWorkspaceView(
              courseId: _course.id,
              courseName: _course.name,
              apiClient: context.read<ApiClient>(),
              onMutation: _refreshCourse,
            ),
          ),
          EntityDetailTab(
            label: 'Objave',
            icon: Icons.campaign,
            builder: (_) => CourseAnnouncementsView(
              courseId: _course.id,
              apiClient: context.read<ApiClient>(),
            ),
          ),
          EntityDetailTab(
            label: 'Upisi',
            icon: Icons.how_to_reg,
            builder: (_) => CourseEnrollmentsView(
              courseId: _course.id,
              onMutation: _refreshCourse,
            ),
          ),
          EntityDetailTab(
            label: 'Rangiranje',
            icon: Icons.leaderboard,
            builder: (_) => CourseRankingView(
              courseId: _course.id,
              reportEndpoint: context.read<RankingProvider>().reportEndpoint(
                _course.id,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _CourseOverview extends StatelessWidget {
  final CourseDto course;

  const _CourseOverview({required this.course});

  @override
  Widget build(BuildContext context) {
    final apiClient = context.read<ApiClient>();

    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.xl),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final image = SizedBox(
            width: AppSize.detailImageWidth,
            child: AspectRatio(
              aspectRatio: 1,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(AppRadius.lg),
                child: networkImageOrPlaceholder(
                  course.imagePath,
                  apiClient,
                  size: double.infinity,
                  borderRadius: AppRadius.lg,
                  placeholder: () => Container(
                    color: AppTheme.background,
                    child: const Center(
                      child: Icon(
                        Icons.class_,
                        size: 48,
                        color: AppTheme.textTertiary,
                      ),
                    ),
                  ),
                ),
              ),
            ),
          );

          final section = FormSection(
            title: 'Podaci o kursu',
            children: [CourseDetailRows(course: course)],
          );

          if (constraints.maxWidth >= 800) {
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              spacing: AppSpacing.xl,
              children: [
                image,
                Expanded(child: section),
              ],
            );
          }

          return Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              image,
              const SizedBox(height: AppSpacing.xl),
              section,
            ],
          );
        },
      ),
    );
  }
}
