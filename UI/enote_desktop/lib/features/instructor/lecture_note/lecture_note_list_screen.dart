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

  /// Defaults to [EntityListPresentation.page] (own Scaffold/AppBar, with a
  /// back/close button when pushed via Navigator). Pass
  /// [EntityListPresentation.embedded] only when the caller already
  /// provides that chrome.
  final EntityListPresentation presentation;
  final VoidCallback? onMutation;

  const LectureNoteListScreen({
    super.key,
    required this.lectureId,
    required this.lectureName,
    this.presentation = EntityListPresentation.page,
    this.onMutation,
  });

  @override
  State<LectureNoteListScreen> createState() => _LectureNoteListScreenState();
}

class _LectureNoteListScreenState extends State<LectureNoteListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<LectureNoteDto>>();

  Future<void> _openForm([LectureNoteDto? existing]) async {
    final provider = context.read<LectureNoteProvider>();
    final saved = await EntityFormScaffold.showAsDialog(
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
    if (saved == true) {
      widget.onMutation?.call();
      _listKey.currentState?.refresh();
    }
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<LectureNoteDto>(
      key: _listKey,
      config: EntityListConfig<LectureNoteDto>(
        title: 'Bilješke — ${widget.lectureName}',
        presentation: widget.presentation,
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
        fetcher: (page, pageSize, search) => context.read<LectureNoteProvider>().search(pagedQuery(page, pageSize, search, searchField: 'title')),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          final provider = context.read<LectureNoteProvider>();
          await provider.remove(item.id);
          widget.onMutation?.call();
          return true;
        },
      ),
    );
  }
}
