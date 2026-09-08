import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import 'course_detail_dialog.dart';
import 'course_form_screen.dart';
import 'course_provider.dart';

class CourseListScreen extends StatefulWidget {
  const CourseListScreen({super.key});

  @override
  State<CourseListScreen> createState() => _CourseListScreenState();
}

class _CourseListScreenState extends State<CourseListScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<CourseDto>>();

  Future<void> _openForm() async {
    final saved = await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => const CourseFormScreen(
        presentation: EntityFormPresentation.dialog,
      ),
    );
    if (saved == true) {
      _gridKey.currentState?.refresh();
    }
  }

  Future<void> _openDetail(CourseDto course) async {
    final changed = await CourseDetailDialog.show(context, course);
    if (changed == true) {
      _gridKey.currentState?.refresh();
    }
  }

  @override
  Widget build(BuildContext context) {
    return EntityGridScreen<CourseDto>(
      key: _gridKey,
      config: EntityGridConfig<CourseDto>(
        placeholderIcon: Icons.class_,
        titleOf: (item) => item.name,
        subtitleOf: (item) => item.isPublished ? 'Aktivan' : 'Neaktivan',
        searchHint: 'Pretraži po nazivu...',
        onAdd: _openForm,
        onTap: (context, item) => _openDetail(item),
        fetcher: (page, pageSize, search) => context
            .read<CourseProvider>()
            .search(pagedQuery(page, pageSize, search, searchField: 'name')),
      ),
    );
  }
}
