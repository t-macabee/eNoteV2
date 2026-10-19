import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_filter_dropdown.dart';
import '../../../widgets/entity_grid_screen.dart';
import 'admin_course_provider.dart';

class AdminCourseListScreen extends StatefulWidget {
  const AdminCourseListScreen({super.key});

  @override
  State<AdminCourseListScreen> createState() => _AdminCourseListScreenState();
}

class _AdminCourseListScreenState extends State<AdminCourseListScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<CourseDto>>();

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
        searchHint: 'Pretraži po nazivu...',
        placeholderIcon: Icons.class_,
        titleOf: (item) => item.name,
        imageUrlOf: (item) => item.imagePath,
        filterBar: EntityFilterDropdown<bool?>(
          label: 'Objavljen',
          width: 220,
          isExpanded: false,
          value: _isPublished,
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
        showAddButton: false,
        fetcher: (page, pageSize, search) => context.read<AdminCourseProvider>().search(pagedQuery(page, pageSize, search, searchField: 'name', filters: {
              if (_isPublished != null) 'isPublished': _isPublished,
})),
      ),
    );
  }
}
