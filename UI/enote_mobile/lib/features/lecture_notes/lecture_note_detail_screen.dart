import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/async_state_view.dart';
import 'lecture_note_provider.dart';

/// S21 — lecture note detail (02 §5 S21): title + selectable body text.
/// The [LectureNoteProvider] is scoped to the `/lectures/notes/detail`
/// route (T60).
class LectureNoteDetailScreen extends StatefulWidget {
  final int noteId;

  const LectureNoteDetailScreen({super.key, required this.noteId});

  @override
  State<LectureNoteDetailScreen> createState() =>
      _LectureNoteDetailScreenState();
}

class _LectureNoteDetailScreenState extends State<LectureNoteDetailScreen> {
  late final Future<LectureNoteDto> _noteFuture;

  @override
  void initState() {
    super.initState();
    _noteFuture = context.read<LectureNoteProvider>().getById(widget.noteId);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Bilješka')),
      body: FutureBuilder<LectureNoteDto>(
        future: _noteFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return AsyncStateView(
              error: snapshot.error,
              onRetry: () => setState(() {
                _noteFuture = context
                    .read<LectureNoteProvider>()
                    .getById(widget.noteId);
              }),
              child: const SizedBox.shrink(),
            );
          }
          final note = snapshot.data;
          if (note == null) {
            return const Center(
              child: Text('Nema rezultata za pretragu.'),
            );
          }
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(
                note.title,
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 12),
              SelectableText(
                note.content,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ],
          );
        },
      ),
    );
  }
}
