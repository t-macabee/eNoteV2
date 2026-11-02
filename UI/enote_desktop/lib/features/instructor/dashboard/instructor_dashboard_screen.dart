import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../shell/role_menu_entries.dart';
import '../../../shell/shell_navigation.dart';
import '../../../widgets/dashboard_count.dart';
import '../../../widgets/dashboard_page.dart';
import '../course/course_provider.dart';
import '../enrollment/instructor_enrollment_provider.dart';
import '../student/instructor_student_provider.dart';

class InstructorDashboardScreen extends StatefulWidget {
  const InstructorDashboardScreen({super.key});

  @override
  State<InstructorDashboardScreen> createState() =>
      _InstructorDashboardScreenState();
}

class _InstructorDashboardScreenState extends State<InstructorDashboardScreen> {
  final _pendingEnrollmentsCount = DashboardCount();
  final _coursesCount = DashboardCount();
  final _studentsCount = DashboardCount();

  @override
  void initState() {
    super.initState();
    _pendingEnrollmentsCount.load(
      () => context.read<InstructorEnrollmentProvider>().countPending(),
    );
    _coursesCount.load(() async {
      final page = await context.read<CourseProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      return page.totalCount;
    });
    _studentsCount.load(() async {
      final page = await context.read<InstructorStudentProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      return page.totalCount;
    });
  }

  @override
  void dispose() {
    _pendingEnrollmentsCount.dispose();
    _coursesCount.dispose();
    _studentsCount.dispose();
    super.dispose();
  }

  void _openEntry(String label) {
    ShellNavigation.of(context).openEntry(label);
  }

  @override
  Widget build(BuildContext context) {
    return DashboardPage(
      cards: [
        DashboardCountCard(
          count: _pendingEnrollmentsCount,
          icon: Icons.assignment_outlined,
          label: 'Zahtjevi na čekanju',
          tone: StatusTone.warning,
          onTap: () => _openEntry(kRequestsEntryLabel),
        ),
        DashboardCountCard(
          count: _coursesCount,
          icon: Icons.class_outlined,
          label: 'Moji kursevi',
          tone: StatusTone.accent,
        ),
        DashboardCountCard(
          count: _studentsCount,
          icon: Icons.school_outlined,
          label: 'Moji studenti',
          tone: StatusTone.info,
        ),
      ],
    );
  }
}
