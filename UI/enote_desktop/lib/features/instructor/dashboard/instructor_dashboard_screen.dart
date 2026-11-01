import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/dashboard_count.dart';
import '../../../widgets/dashboard_page.dart';
import '../course/course_provider.dart';
import '../student/instructor_student_provider.dart';

class InstructorDashboardScreen extends StatefulWidget {
  const InstructorDashboardScreen({super.key});

  @override
  State<InstructorDashboardScreen> createState() =>
      _InstructorDashboardScreenState();
}

class _InstructorDashboardScreenState extends State<InstructorDashboardScreen> {
  final _coursesCount = DashboardCount();
  final _studentsCount = DashboardCount();

  @override
  void initState() {
    super.initState();
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
    _coursesCount.dispose();
    _studentsCount.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return DashboardPage(
      cards: [
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
