import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import '../assignment/assignment_list_screen.dart';
import '../assignment/assignment_provider.dart';
import '../lecture_note/lecture_note_list_screen.dart';
import '../lecture_note/lecture_note_provider.dart';
import 'lecture_attendance_screen.dart';
import 'lecture_form_screen.dart';
import 'lecture_provider.dart';

String _formatDateTime(DateTime d) {
  final day = d.day.toString().padLeft(2, '0');
  final month = d.month.toString().padLeft(2, '0');
  final hour = d.hour.toString().padLeft(2, '0');
  final minute = d.minute.toString().padLeft(2, '0');
  return '$day.$month.${d.year}. $hour:$minute';
}

String _lectureTypeLabel(LectureType type) => switch (type) {
      LectureType.theoretical => 'Teorijsko',
      LectureType.practical => 'Praktično',
      LectureType.combined => 'Kombinovano',
    };

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

  const LectureListScreen({
    super.key,
    required this.courseId,
    required this.courseName,
  });

  @override
  State<LectureListScreen> createState() => _LectureListScreenState();
}

class _LectureListScreenState extends State<LectureListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<LectureDto>>();

  Future<void> _openForm([LectureDto? existing]) async {
    if (existing != null && existing.isCancelled) {
      if (!mounted) return;
      ErrorBanner.show(
        context,
        message: 'Otkazano predavanje se ne može uređivati.',
      );
      return;
    }
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => LectureFormScreen(
          courseId: widget.courseId,
          existing: existing,
        ),
      ),
    );
    _listKey.currentState?.refresh();
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
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Predavanje otkazano.')),
      );
      _listKey.currentState?.refresh();
    } catch (e) {
      if (!mounted) return;
      ErrorBanner.show(context, message: userMessage(e));
    }
  }

  void _openAttendance(LectureDto lecture) {
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => LectureAttendanceScreen(
          lectureId: lecture.id,
          lectureName: lecture.name,
        ),
      ),
    );
  }

  void _openNotes(LectureDto lecture) {
    final apiClient = context.read<ApiClient>();
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => ChangeNotifierProvider<LectureNoteProvider>(
          create: (_) => LectureNoteProvider(
            apiClient: apiClient,
            lectureId: lecture.id,
          ),
          child: LectureNoteListScreen(
            lectureId: lecture.id,
            lectureName: lecture.name,
          ),
        ),
      ),
    );
  }

  void _openAssignments(LectureDto lecture) {
    final apiClient = context.read<ApiClient>();
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => ChangeNotifierProvider<AssignmentProvider>(
          create: (_) => AssignmentProvider(
            apiClient: apiClient,
            lectureId: lecture.id,
          ),
          child: AssignmentListScreen(
            lectureId: lecture.id,
            lectureName: lecture.name,
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<LectureDto>(
      key: _listKey,
      config: EntityListConfig<LectureDto>(
        title: 'Predavanja — ${widget.courseName}',
        columns: [
          ColumnSpec<LectureDto>(
            label: 'Naziv',
            value: (item) => item.name,
          ),
          ColumnSpec<LectureDto>(
            label: 'Tip',
            value: (item) => _lectureTypeLabel(item.lectureType),
          ),
          ColumnSpec<LectureDto>(
            label: 'Vrijeme',
            value: (item) => _formatDateTime(item.lectureTime),
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
        fetcher: (page, pageSize, search) =>
            context.read<LectureProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          'courseId': widget.courseId,
          if (search.isNotEmpty) 'name': search,
        }),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          final provider = context.read<LectureProvider>();
          await provider.remove(item.id);
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
            onPressed: () => _openAttendance(item),
          ),
          IconButton(
            icon: const Icon(Icons.note, size: 18),
            tooltip: 'Bilješke',
            onPressed: () => _openNotes(item),
          ),
          IconButton(
            icon: const Icon(Icons.assignment, size: 18),
            tooltip: 'Zadaci',
            onPressed: () => _openAssignments(item),
          ),
        ],
      ),
    );
  }
}
