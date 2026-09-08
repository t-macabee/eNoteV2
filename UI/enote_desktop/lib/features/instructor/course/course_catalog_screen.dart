import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
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
  }

  Widget _buildFilterBar() {
    return SizedBox(
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
          _gridKey.currentState?.refresh(resetPage: true);
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return EntityGridScreen<CourseDto>(
      key: _gridKey,
      config: EntityGridConfig<CourseDto>(
        placeholderIcon: Icons.class_,
        titleOf: (item) => item.name,
        subtitleOf: (item) => item.instructorName,
        searchHint: 'Pretraži po nazivu...',
        filterBar: _buildFilterBar(),
        showAddButton: false,
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
