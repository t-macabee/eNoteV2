import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_grid_screen.dart';
import 'admin_course_provider.dart';

/// Admin course overview: paged, read-only list across ALL instructors, for
/// cross-system oversight. Course create/edit/delete is Instructor-owned
/// (`CourseController`) — Admin has no write access here. Admins can filter
/// by name (search bar) and published status, and see which instructor owns
/// each course.
class AdminCourseListScreen extends StatefulWidget {
  const AdminCourseListScreen({super.key});

  @override
  State<AdminCourseListScreen> createState() => _AdminCourseListScreenState();
}

class _AdminCourseListScreenState extends State<AdminCourseListScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<CourseDto>>();

  /// null = "Svi" (default) — no published-status filter applied.
  bool? _isPublished;

  void _applyFilters() {
    setState(() {});
    _gridKey.currentState?.refresh(resetPage: true);
  }

  @override
  Widget build(BuildContext context) {
    return EntityGridScreen<CourseDto>(
      key: _gridKey,
      config: EntityGridConfig<CourseDto>(
        title: 'Kursevi',
        searchHint: 'Pretraži po nazivu...',
        placeholderIcon: Icons.class_,
        titleOf: (item) => item.name,
        filterBar: SizedBox(
          width: 220,
          child: DropdownButtonFormField<bool?>(
            initialValue: _isPublished,
            decoration: const InputDecoration(labelText: 'Objavljen'),
            items: const [
              DropdownMenuItem(value: null, child: Text('Svi')),
              DropdownMenuItem(value: false, child: Text('Ne')),
              DropdownMenuItem(value: true, child: Text('Da')),
            ],
            onChanged: (isPublished) {
              _isPublished = isPublished;
              _applyFilters();
            },
          ),
        ),
        showAddButton: false,
        fetcher: (page, pageSize, search) =>
            context.read<AdminCourseProvider>().search({
              'page': page,
              'pageSize': pageSize,
              'includeTotalCount': true,
              if (search.isNotEmpty) 'name': search,
              if (_isPublished != null) 'isPublished': _isPublished,
            }),
      ),
    );
  }
}
