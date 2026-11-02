import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/async_state_view.dart';
import '../../widgets/detail_header.dart';
import 'lecture_note_provider.dart';

class LectureNoteDetailScreen extends StatefulWidget {
  final int noteId;

  const LectureNoteDetailScreen({super.key, required this.noteId});

  @override
  State<LectureNoteDetailScreen> createState() =>
      _LectureNoteDetailScreenState();
}

class _LectureNoteDetailScreenState extends State<LectureNoteDetailScreen> {
  LectureNoteDto? _note;
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final note = await context.read<LectureNoteProvider>().getById(
        widget.noteId,
      );
      if (mounted) {
        setState(() {
          _note = note;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e;
          _loading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final note = _note;
    return Scaffold(
      appBar: AppBar(title: const Text('Bilješka')),
      body: AsyncStateView(
        isLoading: _loading && note == null,
        error: note == null ? _error : null,
        onRetry: _load,
        child: note == null ? const SizedBox.shrink() : _body(note),
      ),
    );
  }

  Widget _body(LectureNoteDto note) {
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.only(bottom: AppSpacing.lg),
        children: [
          DetailHeader(title: note.title),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
            child: SelectableText(
              note.content,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          ),
        ],
      ),
    );
  }
}
