import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../shell/app_router.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/blocked_reason_banner.dart';
import '../../widgets/labeled_value.dart';
import '../lectures/lecture_labels.dart';
import '../lectures/lecture_provider.dart';
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

class _CourseDetailScreenState extends State<CourseDetailScreen> {
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
      params: {
        'courseId': widget.courseId,
        'page': page,
        'pageSize': pageSize,
        'includeTotalCount': true,
        if (search.isNotEmpty) 'name': search,
      },
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
    setState(() => _acting = true);
    try {
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
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
    } finally {
      if (mounted) setState(() => _acting = false);
    }
  }

  Future<void> _unenroll(CourseDto course) async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Ispis sa kursa',
      message: 'Želite li se ispisati sa kursa "${course.name}"?',
    );
    if (confirmed != true || !mounted) return;
    setState(() => _acting = true);
    try {
      await context.read<CourseProvider>().unenroll(course.id);
      // Refetch the master half only: the details half flips to the
      // explained empty state without another lectures request (03 §5.3).
      await _loadCourse();
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
    } finally {
      if (mounted) setState(() => _acting = false);
    }
  }

  void _openRanking(CourseDto course) {
    Navigator.of(
      context,
    ).pushNamed(AppRouter.ranking, arguments: RankingArgs(course.id));
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
        SliverToBoxAdapter(child: _masterCard(session, course, enrolled)),
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
            delegate: _LecturesHeaderDelegate(
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
                return _LectureRow(
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

  Widget _masterCard(
    SessionController session,
    CourseDto course,
    bool enrolled,
  ) {
    final paidUntil = session.membershipPaidUntil;
    final membershipBlocked = !session.isMembershipActive;
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  course.name,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ),
              if (enrolled) const Chip(label: Text('Upisan')),
            ],
          ),
          const SizedBox(height: 12),
          LabeledValue(
            label: 'Instruktor',
            value: orDash(course.instructorName),
          ),
          LabeledValue(
            label: 'Trajanje',
            value:
                '${formatDateNullable(course.startDate)} – '
                '${formatDateNullable(course.endDate)}',
          ),
          LabeledValue(label: 'Cijena', value: formatKM(course.price)),
          LabeledValue(
            label: 'Polaznika',
            value: course.enrolledCount.toString(),
          ),
          if (course.description != null) ...[
            const SizedBox(height: 8),
            Text(
              course.description!,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          ],
          if (membershipBlocked) ...[
            const SizedBox(height: 8),
            BlockedReasonBanner(
              icon: Icons.info_outline,
              reason: paidUntil == null
                  ? 'Članarina nije aktivna.'
                  : 'Članarina je istekla ${formatDate(paidUntil)}. '
                        'Obratite se školi za obnovu.',
            ),
          ],
          const SizedBox(height: 12),
          Row(
            children: [
              if (enrolled)
                OutlinedButton(
                  onPressed: _acting ? null : () => _unenroll(course),
                  child: const Text('Ispiši se'),
                )
              else
                FilledButton(
                  onPressed: (membershipBlocked || _acting)
                      ? null
                      : () => _enroll(course),
                  child: const Text('Upiši se'),
                ),
              const SizedBox(width: 8),
              TextButton(
                onPressed: () => _openRanking(course),
                child: const Text('Rang lista'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Pinned lectures section header: `SectionHeader` copy with a 🔍 action
/// that reveals the lectures `name` search field below it.
class _LecturesHeaderDelegate extends SliverPersistentHeaderDelegate {
  final String title;
  final bool showSearch;
  final VoidCallback onToggleSearch;
  final TextEditingController searchController;
  final Color backgroundColor;
  final TextStyle? textStyle;

  const _LecturesHeaderDelegate({
    required this.title,
    required this.showSearch,
    required this.onToggleSearch,
    required this.searchController,
    required this.backgroundColor,
    required this.textStyle,
  });

  @override
  double get minExtent => showSearch ? 112 : 48;

  @override
  double get maxExtent => showSearch ? 112 : 48;

  @override
  Widget build(
    BuildContext context,
    double shrinkOffset,
    bool overlapsContent,
  ) {
    // Fixed-height equivalent of SectionHeader (uppercase labelSmall +
    // trailing action): SectionHeader's own vertical padding does not fit a
    // 48 px pinned extent, so the row is laid out to exactly 48 px here.
    return Container(
      color: backgroundColor,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            height: 48,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Row(
                children: [
                  Expanded(
                    child: Text(title.toUpperCase(), style: textStyle),
                  ),
                  IconButton(
                    icon: const Icon(Icons.search_outlined),
                    onPressed: onToggleSearch,
                  ),
                ],
              ),
            ),
          ),
          if (showSearch)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: TextField(
                controller: searchController,
                decoration: const InputDecoration(
                  hintText: 'Pretraži…',
                  prefixIcon: Icon(Icons.search_outlined),
                ),
              ),
            ),
        ],
      ),
    );
  }

  @override
  bool shouldRebuild(_LecturesHeaderDelegate oldDelegate) =>
      title != oldDelegate.title || showSearch != oldDelegate.showSearch;
}

class _LectureRow extends StatelessWidget {
  final LectureDto lecture;
  final Future<void> Function() onTap;

  const _LectureRow({required this.lecture, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: ListTile(
        title: Row(
          children: [
            Expanded(
              child: Text(
                formatDateTime(lecture.lectureTime),
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
            ),
            Text(
              lectureStatusLabel(lecture.lectureStatus),
              style: TextStyle(
                color: lecture.lectureStatus == LectureStatus.cancelled
                    ? Theme.of(context).colorScheme.error
                    : Theme.of(context).textTheme.bodyMedium?.color,
              ),
            ),
          ],
        ),
        subtitle: Text(
          '${lecture.name} · '
          '${lectureTypeLabel(lecture.lectureType)} · '
          '${lecture.location}\n'
          '${rsvpStateLabel(lecture.myAttendanceStatus)}',
        ),
        isThreeLine: true,
        trailing: const Icon(Icons.chevron_right),
        onTap: () => onTap(),
      ),
    );
  }
}