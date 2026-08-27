import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import 'instructor_provider.dart';

/// Read-only: `AdminInstructorController` only exposes GetPaged/GetById.
/// Instructor accounts are created via the Users provision flow
/// (`role: "Instructor"`), not here — no add/edit/delete on this screen.
class InstructorListScreen extends StatelessWidget {
  const InstructorListScreen({super.key});

  static String _displayName(InstructorDto item) {
    final name = '${item.firstName ?? ''} ${item.lastName ?? ''}'.trim();
    if (name.isNotEmpty) return name;
    return item.username ?? '-';
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<InstructorDto>(
      config: EntityListConfig<InstructorDto>(
        title: 'Instruktori',
        searchHint: 'Pretraži po imenu...',
        columns: [
          ColumnSpec<InstructorDto>(
            label: 'Ime',
            value: _displayName,
          ),
          ColumnSpec<InstructorDto>(
            label: 'Korisničko ime',
            value: (item) => item.username ?? '-',
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<InstructorProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'name': search,
        }),
        showAddButton: false,
      ),
    );
  }
}
