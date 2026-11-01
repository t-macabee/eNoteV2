import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../shell/shell_navigation.dart';
import '../../../widgets/dashboard_page.dart';
import '../../../widgets/dashboard_stat_card.dart';
import '../course/course_provider.dart';
import '../student/instructor_student_provider.dart';

class InstructorDashboardScreen extends StatefulWidget {
  const InstructorDashboardScreen({super.key});

  @override
  State<InstructorDashboardScreen> createState() =>
      _InstructorDashboardScreenState();
}

class _InstructorDashboardScreenState extends State<InstructorDashboardScreen> {
  int? _coursesCount;
  int? _studentsCount;

  bool _coursesLoading = true;
  bool _studentsLoading = true;

  bool _coursesFailed = false;
  bool _studentsFailed = false;

  @override
  void initState() {
    super.initState();
    _loadCourses();
    _loadStudents();
  }

  Future<void> _loadCourses() async {
    try {
      final page = await context.read<CourseProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      if (!mounted) return;
      setState(() {
        _coursesCount = page.totalCount;
        _coursesLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _coursesFailed = true;
        _coursesLoading = false;
      });
    }
  }

  Future<void> _loadStudents() async {
    try {
      final page = await context.read<InstructorStudentProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      if (!mounted) return;
      setState(() {
        _studentsCount = page.totalCount;
        _studentsLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _studentsFailed = true;
        _studentsLoading = false;
      });
    }
  }

  void _openEntry(String label) {
    ShellNavigation.of(context).openEntry(label);
  }

  @override
  Widget build(BuildContext context) {
    return DashboardPage(
      cards: [
        DashboardStatCard(
          icon: Icons.class_outlined,
          label: 'Moji kursevi',
          value: _coursesCount,
          isLoading: _coursesLoading,
          hasError: _coursesFailed,
          tone: StatusTone.accent,
          onTap: () => _openEntry('Kursevi'),
        ),
        DashboardStatCard(
          icon: Icons.school_outlined,
          label: 'Moji studenti',
          value: _studentsCount,
          isLoading: _studentsLoading,
          hasError: _studentsFailed,
          tone: StatusTone.info,
          onTap: () => _openEntry('Moji studenti'),
        ),
      ],
      shortcuts: [
        DashboardShortcut(
          label: 'Kursevi',
          icon: Icons.class_outlined,
          onTap: () => _openEntry('Kursevi'),
        ),
        DashboardShortcut(
          label: 'Katalog kurseva',
          icon: Icons.menu_book_outlined,
          onTap: () => _openEntry('Katalog kurseva'),
        ),
        DashboardShortcut(
          label: 'Moji studenti',
          icon: Icons.school_outlined,
          onTap: () => _openEntry('Moji studenti'),
        ),
      ],
    );
  }
}
