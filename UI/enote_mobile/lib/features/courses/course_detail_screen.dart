import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../shell/app_router.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/form_submit_state.dart';
import '../lectures/lecture_provider.dart';
import 'course_detail_sections.dart';
import 'course_provider.dart';

/// S15 — the master–details form: course master card + its lectures
/// (02 §4.1). The lectures section is decided by `isEnrolled`, not by list
/// length: a non-enrolled course shows the explained empty state and issues
/// no further lectures requests after the initial load (02 D7).
class CourseDetailScreen extends StatefulWidget {
  final int courseId;

  const CourseDetailScreen({super.key, required this.courseId});

  @override
  State<CourseDetailScreen> createState() => _CourseDetailScreenState();
}

class _CourseDetailScreenState extends State<CourseDetailScreen>
    with FormSubmitState<CourseDetailScreen> {
  late final PagedFetchController<LectureDto> _lecturesController;
  CourseDto? _course;
  Object? _courseError;
  bool _courseLoading = true;
  bool _showSearch = false;
  bool _acting = false;

  @override
  void initState() {
    super.initState();
    _lecturesController = PagedFetchController<LectureDto>(
      fetcher: _fetchLectures,
      pageSize: 20,
      onError: (e) {
        if (mounted) ErrorBanner.show(context, message: userMessage(e));
      },
    );
    _lecturesController.addListener(_onLectures);
    _loadCourse();
    _lecturesController.load();
  }

  Future<PagedResult<LectureDto>> _fetchLectures(
    int page,
    int pageSize,
    String search,
  ) {
    return context.read<LectureProvider>().getPage(
      params: pagedQuery(
        page,
        pageSize,
        search,
        searchField: 'name',
        filters: {'courseId': widget.courseId},
      ),
    );
  }

  void _onLectures() {
    if (mounted) setState(() {});
  }

  @override
  void dispose() {
    _lecturesController.removeListener(_onLectures);
    _lecturesController.dispose();
    super.dispose();
  }

  Future<void> _loadCourse() async {
    setState(() {
      _courseLoading = true;
      _courseError = null;
    });
    try {
      final course = await context.read<CourseProvider>().getById(
        widget.courseId,
      );
      if (mounted) {
        setState(() {
          _course = course;
          _courseLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _courseError = e;
          _courseLoading = false;
        });
      }
    }
  }

  Future<void> _enroll(CourseDto course) async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Upis na kurs',
      message: 'Želite li se upisati na kurs "${course.name}"?',
    );
    if (confirmed != true || !mounted) return;
    await submitWith(
      (busy) => _acting = busy,
      () async {
        await context.read<CourseProvider>().enroll(course.id);
        await _loadCourse();
        _lecturesController.refresh(resetPage: true);
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                'Uspješno ste upisani na kurs ${course.name}.',
              ),
            ),
          );
        }
        // An enrollment starts unpaid: send the student straight to tuition
        // unless the course is free (decision 7). Reload on return so the
        // banner flips to `Plaćeno do`.
        final enrolledCourse = _course;
        if (mounted &&
            enrolledCourse != null &&
            !enrolledCourse.isFree &&
            enrolledCourse.enrollmentId != null) {
          await _openTuition(enrolledCourse);
        }
      },
      showBanner: true,
    );
  }

  Future<void> _unenroll(CourseDto course) async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Ispis sa kursa',
      message: 'Želite li se ispisati sa kursa "${course.name}"?',
    );
    if (confirmed != true || !mounted) return;
    await submitWith(
      (busy) => _acting = busy,
      () async {
        await context.read<CourseProvider>().unenroll(course.id);
        // Refetch the master half only: the details half flips to the
        // explained empty state without another lectures request (03 §5.3).
        await _loadCourse();
      },
      showBanner: true,
    );
  }

  void _openRanking(CourseDto course) {
    Navigator.of(
      context,
    ).pushNamed(AppRouter.ranking, arguments: RankingArgs(course.id));
  }

  Future<void> _openTuition(CourseDto course) async {
    final enrollmentId = course.enrollmentId;
    if (enrollmentId == null) return;
    await Navigator.of(context).pushNamed(
      AppRouter.tuitionPayment,
      arguments: TuitionArgs(
        enrollmentId,
        courseName: course.name,
        price: course.price,
      ),
    );
    // Always reload: Android back pops a null result from the succeeded view
    // (the close icon is hidden but `PopScope.canPop` is true), so keying the
    // reload on `pop(true)` would leave the banner stuck on "nije plaćena".
    if (!mounted) return;
    await _loadCourse();
  }

  @override
  Widget build(BuildContext context) {
    final course = _course;
    return Scaffold(
      appBar: AppBar(title: const Text('Kurs')),
      body: AsyncStateView(
        isLoading: _courseLoading && course == null,
        error: course == null ? _courseError : null,
        onRetry: () {
          _loadCourse();
          _lecturesController.load();
        },
        child: course == null
            ? const SizedBox.shrink()
            : _body(context, course),
      ),
    );
  }

  Widget _body(BuildContext context, CourseDto course) {
    final session = context.watch<SessionController>();
    final enrolled = course.isEnrolled;
    final lectures = _lecturesController.items;
    final totalCount = _lecturesController.totalCount;
    return CustomScrollView(
      slivers: [
        SliverToBoxAdapter(
          child: CourseMasterCard(
            course: course,
            enrolled: enrolled,
            membershipBlocked: !session.isMembershipActive,
            membershipPaidUntil: session.membershipPaidUntil,
            isActing: _acting,
            onEnroll: () => _enroll(course),
            onUnenroll: () => _unenroll(course),
            onRanking: () => _openRanking(course),
            onTuition: () => _openTuition(course),
          ),
        ),
        if (!enrolled)
          const SliverToBoxAdapter(
            child: Padding(
              padding: EdgeInsets.all(24),
              child: Text(
                'Upišite se da vidite predavanja ovog kursa.',
                textAlign: TextAlign.center,
              ),
            ),
          )
        else ...[
          SliverPersistentHeader(
            pinned: true,
            delegate: CourseLecturesHeader(
              title: 'Predavanja · ${totalCount ?? lectures.length}',
              showSearch: _showSearch,
              onToggleSearch: () =>
                  setState(() => _showSearch = !_showSearch),
              searchController: _lecturesController.searchController,
              backgroundColor: Theme.of(context).scaffoldBackgroundColor,
              textStyle: Theme.of(context).textTheme.labelSmall,
            ),
          ),
          SliverList(
            delegate: SliverChildBuilderDelegate(
              (context, index) {
                final lecture = lectures[index];
                return CourseLectureRow(
                  lecture: lecture,
                  onTap: () async {
                    await Navigator.of(context).pushNamed(
                      AppRouter.lectureDetail,
                      arguments: LectureDetailArgs(lecture.id),
                    );
                    // The RSVP answer may have changed inside S18 — refetch
                    // the current lectures page so the row shows the same
                    // state on return (03 §5.3 step 5). Never fires while
                    // not enrolled: rows (and their taps) don't exist then.
                    if (!context.mounted) return;
                    if (_course?.isEnrolled ?? false) {
                      _lecturesController.refresh();
                    }
                  },
                );
              },
              childCount: lectures.length,
            ),
          ),
          SliverToBoxAdapter(
            child: PagedPaginationBar(controller: _lecturesController),
          ),
        ],
      ],
    );
  }
}