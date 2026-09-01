import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'admin_course_form_screen.dart';
import 'admin_course_provider.dart';

/// Admin course overview: paged list across ALL instructors.
///
/// Admins can create a course on an instructor's behalf (e.g. account-access
/// issues) and assign it to a specific instructor. Edit/delete stays
/// Instructor-owned. Admins can filter by name (search bar) and published
/// status, and see which instructor owns each course.
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

  Future<void> _openForm() async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => const AdminCourseFormScreen(
        presentation: EntityFormPresentation.dialog,
      ),
    );
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
        showAddButton: true,
        inlineToolbar: true,
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
        onAdd: _openForm,
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
