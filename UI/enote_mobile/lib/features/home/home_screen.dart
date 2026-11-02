import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../shell/routes.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/detail_section.dart';
import '../assignments/assignment_provider.dart';
import '../assignments/assignment_tile.dart';
import '../courses/course_provider.dart';
import '../courses/course_tile.dart';
import '../lectures/lecture_provider.dart';
import '../lectures/lecture_tile.dart';
import '../rentals/rental_provider.dart';
import '../rentals/rental_tile.dart';
import '../student_notices.dart';
import 'home_provider.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late final List<ChangeNotifier> _reloadSources;
  StudentHomeDto? _home;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _reloadSources = [
      context.read<RentalProvider>(),
      context.read<CourseProvider>(),
      context.read<LectureProvider>(),
      context.read<AssignmentProvider>(),
    ];
    for (final source in _reloadSources) {
      source.addListener(_load);
    }
    _load();
  }

  @override
  void dispose() {
    for (final source in _reloadSources) {
      source.removeListener(_load);
    }
    super.dispose();
  }

  Future<void> _load() async {
    if (_error != null) setState(() => _error = null);
    try {
      final home = await context.read<HomeProvider>().load();
      if (mounted) setState(() => _home = home);
    } catch (e) {
      if (!mounted) return;
      if (_home == null) {
        setState(() => _error = e);
      } else {
        ErrorBanner.show(context, message: userMessage(e));
      }
    }
  }

  Future<void> _open(String route, Object arguments) async {
    await Navigator.of(context).pushNamed(route, arguments: arguments);
    if (mounted) _load();
  }

  String _greeting(SessionController session) {
    final profile = session.profile;
    final firstName = profile?.profile.firstName?.trim();
    final name = firstName != null && firstName.isNotEmpty
        ? firstName
        : profile?.username;
    return name == null || name.isEmpty ? 'Dobrodošli' : 'Dobrodošli, $name';
  }

  Widget _emptyText(String message) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Text(
        message,
        style: Theme.of(context).textTheme.bodyMedium
            ?.copyWith(color: AppTheme.textSecondary),
      ),
    );
  }

  Widget _tileList(List<Widget> rows) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var i = 0; i < rows.length; i++) ...[
          if (i > 0) const Divider(height: 1),
          rows[i],
        ],
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final home = _home;
    if (home == null) {
      return AsyncStateView(
        isLoading: _error == null,
        error: _error,
        onRetry: _load,
        child: const SizedBox.shrink(),
      );
    }
    final session = context.watch<SessionController>();
    final nextLecture = home.nextLecture;
    final assignments = home.openAssignments;
    final rentalId = home.debt.rentalId;
    final banners = [
      if (session.profile != null && !session.isMembershipActive)
        StatusBanner(
          icon: Icons.warning_amber_outlined,
          message: membershipNotice(session.membershipPaidUntil),
          tone: StatusTone.danger,
        ),
      if (home.debt.hasUnpaidDebt)
        StatusBanner(
          icon: Icons.warning_amber_outlined,
          message: rentalDebtNotice,
          tone: StatusTone.danger,
          actionLabel: 'Plati sada',
          onAction: rentalId == null
              ? null
              : () => _open(AppRoutes.payment, PaymentArgs(rentalId)),
        ),
    ];

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.only(bottom: AppSpacing.lg),
        children: [
          Padding(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: Text(
              _greeting(session),
              style: Theme.of(context).textTheme.headlineSmall,
            ),
          ),
          if (banners.isNotEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                0,
                AppSpacing.lg,
                AppSpacing.lg,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                spacing: AppSpacing.sm,
                children: banners,
              ),
            ),
          DetailSection(
            title: 'Sljedeće predavanje',
            child: nextLecture == null
                ? _emptyText('Nema zakazanih predavanja.')
                : LectureTile(
                    lecture: nextLecture,
                    onTap: () => _open(
                      AppRoutes.lectureDetail,
                      LectureDetailArgs(nextLecture.id),
                    ),
                  ),
          ),
          DetailSection(
            title: 'Otvoreni zadaci',
            child: assignments.isEmpty
                ? _emptyText('Nema otvorenih zadataka.')
                : _tileList([
                    for (final assignment in assignments)
                      AssignmentTile(
                        assignment: assignment,
                        onTap: () => _open(
                          AppRoutes.assignmentDetail,
                          AssignmentDetailArgs(assignment.id),
                        ),
                      ),
                  ]),
          ),
          DetailSection(
            title: 'Moja iznajmljivanja',
            child: home.currentRentals.isEmpty
                ? _emptyText('Nemate aktivnih iznajmljivanja.')
                : _tileList([
                    for (final rental in home.currentRentals)
                      RentalTile(
                        rental: rental,
                        onTap: () => _open(
                          AppRoutes.rentalDetail,
                          RentalDetailArgs(rental.id),
                        ),
                      ),
                  ]),
          ),
          if (home.tuitionDueCourses.isNotEmpty)
            DetailSection(
              title: 'Školarina',
              child: _tileList([
                for (final course in home.tuitionDueCourses)
                  CourseTile(
                    course: course,
                    subtitle: tuitionDueNotice(course.paidUntil),
                    onTap: () => _open(
                      AppRoutes.courseDetail,
                      CourseDetailArgs(course.id),
                    ),
                  ),
              ]),
            ),
        ],
      ),
    );
  }
}
