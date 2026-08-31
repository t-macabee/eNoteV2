import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import 'admin_course_provider.dart';

/// Read-only admin course overview: paged list across ALL instructors.
///
/// No Add button and no edit/delete row actions — course create/update/delete
/// stays Instructor-owned. Admins can filter by name (search bar) and
/// published status, and see which instructor owns each course.
class AdminCourseListScreen extends StatefulWidget {
  const AdminCourseListScreen({super.key});

  @override
  State<AdminCourseListScreen> createState() => _AdminCourseListScreenState();
}

class _AdminCourseListScreenState extends State<AdminCourseListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<CourseDto>>();

  /// null = "Svi" (default) — no published-status filter applied.
  bool? _isPublished;

  void _applyFilters() {
    setState(() {});
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<CourseDto>(
      key: _listKey,
      config: EntityListConfig<CourseDto>(
        title: 'Kursevi',
        searchHint: 'Pretraži po nazivu...',
        columns: [
          ColumnSpec<CourseDto>(
            label: 'Naziv',
            value: (item) => item.name,
          ),
          ColumnSpec<CourseDto>(
            label: 'Instruktor',
            value: (item) => item.instructorName ?? '-',
          ),
          ColumnSpec<CourseDto>(
            label: 'Cijena',
            value: (item) => item.price.toStringAsFixed(2),
          ),
          ColumnSpec<CourseDto>(
            label: 'Objavljen',
            value: (item) => item.isPublished ? 'Da' : 'Ne',
          ),
          ColumnSpec<CourseDto>(
            label: 'Broj upisanih',
            value: (item) => item.enrolledCount,
          ),
          ColumnSpec<CourseDto>(
            label: 'Datum početka',
            value: (item) => formatDateNullable(item.startDate),
          ),
        ],
        showAddButton: false,
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
