import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'lecture_form_screen.dart';
import 'lecture_provider.dart';
import 'lecture_type_label.dart';




enum LectureSubView {
  attendance,
  notes,
  assignments,
}

String _lectureStatusLabel(LectureDto lecture) {
  if (lecture.isCancelled) return 'Otkazano';
  return switch (lecture.lectureStatus) {
    LectureStatus.scheduled => 'Zakazano',
    LectureStatus.held => 'Održano',
    LectureStatus.cancelled => 'Otkazano',
  };
}

class LectureListScreen extends StatefulWidget {
  final int courseId;
  final String courseName;

  /// Defaults to [EntityListPresentation.page] (own Scaffold/AppBar, with a
  /// back/close button when pushed via Navigator). Pass
  /// [EntityListPresentation.embedded] only when the caller already
  /// provides that chrome.
  final EntityListPresentation presentation;
  final EntityListStyle listStyle;
  final void Function(LectureDto lecture, LectureSubView subView)? onOpenSubView;
  final VoidCallback? onMutation;

  const LectureListScreen({
    super.key,
    required this.courseId,
    required this.courseName,
    this.presentation = EntityListPresentation.page,
    this.listStyle = EntityListStyle.tiles,
    this.onOpenSubView,
    this.onMutation,
  });

  @override
  State<LectureListScreen> createState() => _LectureListScreenState();
}

class _LectureListScreenState extends State<LectureListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<LectureDto>>();

  Future<void> _openForm([LectureDto? existing]) async {
    final saved = await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => LectureFormScreen(
        courseId: widget.courseId,
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    if (saved == true) {
      widget.onMutation?.call();
      _listKey.currentState?.refresh();
    }
  }

  Future<void> _cancelLecture(LectureDto lecture) async {
    if (lecture.isCancelled) return;
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrdite otkazivanje',
      message:
          'Da li ste sigurni da želite otkazati ovo predavanje? Svi upisani studenti biće obaviješteni.',
    );
    if (confirmed != true) return;
    if (!mounted) return;
    try {
      await context.read<LectureProvider>().cancel(lecture.id);
      if (!mounted) return;
      widget.onMutation?.call();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Predavanje otkazano.')),
      );
      _listKey.currentState?.refresh();
    } catch (e) {
      if (!mounted) return;
      ErrorBanner.show(context, message: userMessage(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<LectureDto>(
      key: _listKey,
      config: EntityListConfig<LectureDto>(
        title: 'Predavanja — ${widget.courseName}',
        presentation: widget.presentation,
        listStyle: widget.listStyle,
        rowIcon: Icons.event_note,
        columns: [
          ColumnSpec<LectureDto>(
            label: 'Naziv',
            value: (item) => item.name,
          ),
          ColumnSpec<LectureDto>(
            label: 'Tip',
            value: (item) => lectureTypeLabel(item.lectureType),
          ),
          ColumnSpec<LectureDto>(
            label: 'Vrijeme',
            value: (item) => formatDateTime(item.lectureTime),
          ),
          ColumnSpec<LectureDto>(
            label: 'Trajanje (min)',
            value: (item) => item.duration,
          ),
          ColumnSpec<LectureDto>(
            label: 'Kapacitet',
            value: (item) => item.capacity?.toString() ?? '-',
          ),
          ColumnSpec<LectureDto>(
            label: 'Polaznika',
            value: (item) => item.attendeeCount,
          ),
          ColumnSpec<LectureDto>(
            label: 'Status',
            value: (item) => _lectureStatusLabel(item),
            style: (item) {
              if (item.isCancelled) {
                return const TextStyle(color: Colors.red, fontWeight: FontWeight.bold);
              }
              if (item.lectureStatus == LectureStatus.held) {
                return const TextStyle(color: Colors.green);
              }
              return null;
            },
          ),
        ],
        fetcher: (page, pageSize, search) => context.read<LectureProvider>().search(pagedQuery(page, pageSize, search, searchField: 'name', filters: {
          'courseId': widget.courseId,
})),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          final provider = context.read<LectureProvider>();
          await provider.remove(item.id);
          widget.onMutation?.call();
          return true;
        },
        extraActions: (context, item) => [
          IconButton(
            icon: Icon(
              Icons.cancel,
              size: 18,
              color: item.isCancelled ? Colors.grey : Colors.orange,
            ),
            tooltip: item.isCancelled ? 'Već otkazano' : 'Otkaži',
            onPressed: item.isCancelled ? null : () => _cancelLecture(item),
          ),
          IconButton(
            icon: const Icon(Icons.how_to_reg, size: 18),
            tooltip: 'Prisustvo',
            onPressed: widget.onOpenSubView == null
                ? null
                : () => widget.onOpenSubView!(item, LectureSubView.attendance),
          ),
          IconButton(
            icon: const Icon(Icons.note, size: 18),
            tooltip: 'Bilješke',
            onPressed: widget.onOpenSubView == null
                ? null
                : () => widget.onOpenSubView!(item, LectureSubView.notes),
          ),
          IconButton(
            icon: const Icon(Icons.assignment, size: 18),
            tooltip: 'Zadaci',
            onPressed: widget.onOpenSubView == null
                ? null
                : () => widget.onOpenSubView!(item, LectureSubView.assignments),
          ),
        ],
      ),
    );
  }
}
