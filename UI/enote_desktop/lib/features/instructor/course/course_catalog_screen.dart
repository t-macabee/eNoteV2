import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_filter_dropdown.dart';
import '../../../widgets/entity_grid_screen.dart';
import 'course_catalog_detail_dialog.dart';
import 'course_catalog_provider.dart';

class CourseCatalogScreen extends StatefulWidget {
  const CourseCatalogScreen({super.key});

  @override
  State<CourseCatalogScreen> createState() => _CourseCatalogScreenState();
}

class _CourseCatalogScreenState extends State<CourseCatalogScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<CourseDto>>();

  int? _selectedInstructorId;
  List<CourseCatalogInstructorDto> _instructors = [];
  bool _instructorsLoading = true;
  String? _instructorsError;
  CourseCatalogSummaryDto? _summary;

  @override
  void initState() {
    super.initState();
    _loadInstructors();
    _loadSummary();
  }

  Future<void> _loadInstructors() async {
    setState(() {
      _instructorsLoading = true;
      _instructorsError = null;
    });
    try {
      final instructors =
          await context.read<CourseCatalogProvider>().getCatalogInstructors();
      if (!mounted) return;
      setState(() {
        _instructors = instructors;
        _instructorsLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _instructorsError = 'Filteri nisu učitani.';
        _instructorsLoading = false;
      });
    }
  }

  Future<void> _loadSummary() async {
    try {
      final summary =
          await context.read<CourseCatalogProvider>().getCatalogSummary();
      if (!mounted) return;
      setState(() {
        _summary = summary;
      });
    } catch (_) {
    }
  }

  Widget _buildFilterBar() {
    return EntityFilterDropdown<int?>(
      label: 'Instruktor',
      width: 240,
      outlined: true,
      value: _selectedInstructorId,
      isLoading: _instructorsLoading,
      errorText: _instructorsError,
      onRetry: _loadInstructors,
      items: [
        const DropdownMenuItem(value: null, child: Text('Svi instruktori')),
        for (final instructor in _instructors)
          DropdownMenuItem(
            value: instructor.id,
            child: Text(instructor.name ?? 'Nepoznat korisnik'),
          ),
      ],
      onChanged: (value) {
        setState(() => _selectedInstructorId = value);
        _gridKey.currentState?.refresh(resetPage: true);
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return EntityGridScreen<CourseDto>(
      key: _gridKey,
      config: EntityGridConfig<CourseDto>(
        placeholderIcon: Icons.class_,
        titleOf: (item) => item.name,
        imageUrlOf: (item) => item.imagePath,
        subtitleOf: (item) => item.instructorName,
        searchHint: 'Pretraži po nazivu...',
        filterBar: _buildFilterBar(),
        showAddButton: false,
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
        onTap: (context, item) => CourseCatalogDetailDialog.show(context, item),
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
