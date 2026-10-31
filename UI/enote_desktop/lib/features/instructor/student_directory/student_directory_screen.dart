import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_grid_screen.dart';
import 'student_directory_provider.dart';
import 'student_profile_dialog.dart';

class StudentDirectoryScreen extends StatelessWidget {
  const StudentDirectoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final provider = context.read<StudentDirectoryProvider>();

    return EntityGridScreen<StudentDirectoryEntryDto>(
      config: EntityGridConfig<StudentDirectoryEntryDto>(
        fetcher: (page, pageSize, search) => provider.search(
          pagedQuery(page, pageSize, search, searchField: 'name'),
        ),
        titleOf: (item) =>
            formatDisplayName(item.firstName, item.lastName, item.username),
        subtitleOf: (item) => item.username != null ? '@${item.username}' : null,
        placeholderIcon: Icons.person_outline,
        imageUrlOf: (item) =>
            userPictureUrl(context.read<ApiClient>(), item.appUserId),
        onTap: (context, item) => showDialog<void>(
          context: context,
          builder: (_) => StudentProfileDialog(student: item),
        ),
        showAddButton: false,
        searchHint: 'Pretraži sve studente po imenu ili korisničkom imenu...',
      ),
    );
  }
}
