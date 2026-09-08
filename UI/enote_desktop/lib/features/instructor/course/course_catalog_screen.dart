import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import 'course_catalog_provider.dart';

class CourseCatalogScreen extends StatefulWidget {
  const CourseCatalogScreen({super.key});

  @override
  State<CourseCatalogScreen> createState() => _CourseCatalogScreenState();
}

class _CourseCatalogScreenState extends State<CourseCatalogScreen> {
  final _listKey = GlobalKey<EntityListScreenState<CourseDto>>();

  int? _selectedInstructorId;
  List<CourseCatalogInstructorDto> _instructors = [];
  CourseCatalogSummaryDto? _summary;

  @override
  void initState() {
    super.initState();
    _loadInitialData();
  }

  Future<void> _loadInitialData() async {
    final provider = context.read<CourseCatalogProvider>();
    try {
      final instructors = await provider.getCatalogInstructors();
      if (mounted) {
        setState(() {
          _instructors = instructors;
        });
      }
    } catch (_) {
      // Catalog still works without instructor dropdown items.
    }

    try {
      final summary = await provider.getCatalogSummary();
      if (mounted) {
        setState(() {
          _summary = summary;
        });
      }
    } catch (_) {
      // If the call fails, render nothing — never block the catalog on it.
    }
  }

  Widget _buildFilterBar() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          SizedBox(
            width: 240,
            child: DropdownButtonFormField<int?>(
              isExpanded: true,
              initialValue: _selectedInstructorId,
              decoration: const InputDecoration(
                labelText: 'Instruktor',
                border: OutlineInputBorder(),
              ),
              items: [
                const DropdownMenuItem(value: null, child: Text('Svi instruktori')),
                for (final instructor in _instructors)
                  DropdownMenuItem(
                    value: instructor.id,
                    child: Text(instructor.name ?? 'Instruktor #${instructor.id}'),
                  ),
              ],
              onChanged: (value) {
                setState(() => _selectedInstructorId = value);
                _listKey.currentState?.refresh(resetPage: true);
              },
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<CourseDto>(
      key: _listKey,
      config: EntityListConfig<CourseDto>(
        title: 'Katalog kurseva',
        searchHint: 'Pretraži po nazivu...',
        filterBar: _buildFilterBar(),
        showAddButton: false,
        onEdit: null,
        onDelete: null,
        extraActions: null,
        trailing: _summary != null
            ? Center(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  child: Text(
                    '${_summary!.totalCourses} kurseva · ${_summary!.totalStudents} studenata',
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                ),
              )
            : null,
        columns: [
          ColumnSpec<CourseDto>(
            label: 'Naziv',
            value: (item) => item.name,
          ),
          ColumnSpec<CourseDto>(
            label: 'Instruktor',
            value: (item) => item.instructorName ?? '—',
          ),
          ColumnSpec<CourseDto>(
            label: 'Cijena',
            value: (item) => item.price.toStringAsFixed(2),
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
        fetcher: (page, pageSize, search) => context
            .read<CourseCatalogProvider>()
            .search(pagedQuery(
              page,
              pageSize,
              search,
              searchField: 'name',
              filters: {
                if (_selectedInstructorId != null)
                  'instructorId': _selectedInstructorId,
              },
            )),
      ),
    );
  }
}
