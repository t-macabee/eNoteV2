import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';

/// Placeholder for the admin-facing "Courses" tab (cross-instructor view).
///
/// `CourseController` only exposes Instructor/Student-scoped actions today —
/// there is no admin-scoped course list endpoint to build a real screen
/// against yet (Admin IA rework prompt, point 4). Field/filter shape is
/// intentionally not invented here; build the real screen once that
/// endpoint exists.
class AdminCourseStubScreen extends StatelessWidget {
  const AdminCourseStubScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Kursevi')),
      body: const Center(
        child: Padding(
          padding: EdgeInsets.all(32),
          child: Text(
            'Admin pregled kurseva još nije dostupan — backend nema '
            'admin-scoped endpoint za listu kurseva.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppTheme.textSecondary),
          ),
        ),
      ),
    );
  }
}
