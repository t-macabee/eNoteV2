import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'lecture_note_form_screen.dart';
import 'lecture_note_provider.dart';



class LectureNoteListScreen extends StatefulWidget {
  final int lectureId;
  final String lectureName;

  const LectureNoteListScreen({
    super.key,
    required this.lectureId,
    required this.lectureName,
  });

  @override
  State<LectureNoteListScreen> createState() => _LectureNoteListScreenState();
}

class _LectureNoteListScreenState extends State<LectureNoteListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<LectureNoteDto>>();

  Future<void> _openForm([LectureNoteDto? existing]) async {
    final provider = context.read<LectureNoteProvider>();
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => ChangeNotifierProvider<LectureNoteProvider>.value(
        value: provider,
        child: LectureNoteFormScreen(
          lectureId: widget.lectureId,
          existing: existing,
          presentation: EntityFormPresentation.dialog,
        ),
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<LectureNoteDto>(
      key: _listKey,
      config: EntityListConfig<LectureNoteDto>(
        title: 'Bilješke — ${widget.lectureName}',
        columns: [
          ColumnSpec<LectureNoteDto>(
            label: 'Naslov',
            value: (item) => item.title,
          ),
          ColumnSpec<LectureNoteDto>(
            label: 'Sadržaj',
            value: (item) => truncate(item.content, 80),
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<LectureNoteProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'title': search,
        }),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          final provider = context.read<LectureNoteProvider>();
          await provider.remove(item.id);
          return true;
        },
      ),
    );
  }
}
