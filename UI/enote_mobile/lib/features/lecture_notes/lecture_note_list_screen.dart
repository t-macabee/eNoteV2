import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/paged_list_view.dart';
import 'lecture_note_provider.dart';

/// S20 — lecture notes list (02 §5 S20). The [LectureNoteProvider] is
/// scoped to the `/lectures/notes` route (T60).
class LectureNoteListScreen extends StatefulWidget {
  final int lectureId;

  const LectureNoteListScreen({super.key, required this.lectureId});

  @override
  State<LectureNoteListScreen> createState() => _LectureNoteListScreenState();
}

class _LectureNoteListScreenState extends State<LectureNoteListScreen> {
  late final PagedFetchController<LectureNoteDto> _controller;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<LectureNoteDto>(
      fetcher: _fetch,
      pageSize: 20,
    );
  }

  Future<PagedResult<LectureNoteDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    return context.read<LectureNoteProvider>().getPage(
      params: pagedQuery(page, pageSize, search, searchField: 'title'),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Bilješke')),
      body: PagedListView<LectureNoteDto>(
        controller: _controller,
        itemBuilder: (context, note) => ListTile(
          leading: const Icon(Icons.notes_outlined),
          title: Text(note.title),
          subtitle: Text(truncate(note.content, 80)),
          onTap: () => Navigator.of(context).pushNamed(
            AppRouter.lectureNoteDetail,
            arguments: LectureNoteDetailArgs(widget.lectureId, note.id),
          ),
        ),
      ),
    );
  }
}
