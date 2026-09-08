import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import 'instructor_student_details_dialog.dart';
import 'instructor_student_form_screen.dart';
import 'instructor_student_provider.dart';

class InstructorStudentListScreen extends StatefulWidget {
  const InstructorStudentListScreen({super.key});

  @override
  State<InstructorStudentListScreen> createState() =>
      _InstructorStudentListScreenState();
}

class _InstructorStudentListScreenState
    extends State<InstructorStudentListScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<StudentDto>>();

  Future<void> _openCreateForm() async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => const InstructorStudentFormScreen(
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _gridKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<InstructorStudentProvider>();

    return EntityGridScreen<StudentDto>(
      key: _gridKey,
      config: EntityGridConfig<StudentDto>(
        fetcher: (page, pageSize, search) => provider.search(
          pagedQuery(page, pageSize, search, searchField: 'name')
        ),
        titleOf: (item) =>
            formatDisplayName(item.firstName, item.lastName, item.username),
        subtitleOf: (item) => item.username != null ? '@${item.username}' : null,
        placeholderIcon: Icons.school_outlined,
        imageUrlOf: (item) =>
            userPictureUrl(context.read<ApiClient>(), item.appUserId),
        onTap: (context, item) => showDialog<void>(
          context: context,
          builder: (_) => InstructorStudentDetailsDialog(student: item),
        ),
        onAdd: _openCreateForm,
        addLabel: 'Kreiraj studenta',
        searchHint: 'Pretraži studente po imenu ili korisničkom imenu...',
      ),
    );
  }
}
