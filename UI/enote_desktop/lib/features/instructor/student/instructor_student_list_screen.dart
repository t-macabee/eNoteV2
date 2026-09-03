import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_grid_screen.dart';
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

  static String _formatDisplayName(
    String? firstName,
    String? lastName,
    String? username,
  ) {
    final name = '${firstName ?? ''} ${lastName ?? ''}'.trim();
    if (name.isNotEmpty) return name;
    return username ?? '-';
  }

  Future<void> _openCreateForm() async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => const InstructorStudentFormScreen(),
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
        title: 'Studenti',
        fetcher: (page, pageSize, search) => provider.search(
          search.isEmpty
              ? {'page': page, 'pageSize': pageSize}
              : {'name': search, 'page': page, 'pageSize': pageSize},
        ),
        titleOf: (item) =>
            _formatDisplayName(item.firstName, item.lastName, item.username),
        subtitleOf: (item) => item.username != null ? '@${item.username}' : null,
        placeholderIcon: Icons.school_outlined,
        onAdd: _openCreateForm,
        addLabel: 'Kreiraj studenta',
        searchHint: 'Pretraži studente po imenu ili korisničkom imenu...',
      ),
    );
  }
}
