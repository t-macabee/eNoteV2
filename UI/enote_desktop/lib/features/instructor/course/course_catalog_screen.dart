import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../../../widgets/inline_error_text.dart';
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
  CourseCatalogSummaryDto? _summary;
  String? _summaryError;

  @override
  void initState() {
    super.initState();
    _loadSummary();
  }

  Future<void> _loadSummary() async {
    try {
      final summary =
          await context.read<CourseCatalogProvider>().getCatalogSummary();
      if (!mounted) return;
      setState(() {
        _summary = summary;
        _summaryError = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _summaryError = userMessage(e);
      });
    }
  }

  Widget _buildFilterBar() {
    return AsyncDropdown<CourseCatalogInstructorDto>(
      label: 'Instruktor',
      width: 240,
      emptyOptionLabel: 'Svi instruktori',
      value: _selectedInstructorId,
      fetcher: () async {
        final instructorProvider = context.read<CourseCatalogProvider>();
        final instructors = await fetchAllPages(
          (query) => instructorProvider.getCatalogInstructors(params: query),
        );
        return instructors
          ..sort((a, b) => (a.name ?? '').compareTo(b.name ?? ''));
      },
      itemLabel: (instructor) => instructor.name ?? 'Nepoznat korisnik',
      itemId: (instructor) => instructor.id,
      onChanged: (value, _) {
        setState(() => _selectedInstructorId = value as int?);
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
            : _summaryError != null
                ? Center(
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      child: InlineErrorText(_summaryError!),
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
