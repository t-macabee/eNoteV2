import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import '../assignment_submission/submission_list_screen.dart';
import '../assignment_submission/submission_provider.dart';
import 'assignment_form_screen.dart';
import 'assignment_provider.dart';



class AssignmentListScreen extends StatefulWidget {
  final int lectureId;
  final String lectureName;

  const AssignmentListScreen({
    super.key,
    required this.lectureId,
    required this.lectureName,
  });

  @override
  State<AssignmentListScreen> createState() => _AssignmentListScreenState();
}

class _AssignmentListScreenState extends State<AssignmentListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<AssignmentDto>>();

  void _openSubmissions(AssignmentDto assignment) {
    final apiClient = context.read<ApiClient>();
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => ChangeNotifierProvider<SubmissionProvider>(
          create: (_) => SubmissionProvider(
            apiClient: apiClient,
            lectureId: widget.lectureId,
            assignmentId: assignment.id,
          ),
          child: SubmissionListScreen(
            lectureId: widget.lectureId,
            assignmentId: assignment.id,
            assignmentTitle: assignment.title,
          ),
        ),
      ),
    );
  }

  Future<void> _openForm([AssignmentDto? existing]) async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => ChangeNotifierProvider<AssignmentProvider>.value(
          value: context.read<AssignmentProvider>(),
          child: AssignmentFormScreen(
            lectureId: widget.lectureId,
            existing: existing,
          ),
        ),
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<AssignmentDto>(
      key: _listKey,
      config: EntityListConfig<AssignmentDto>(
        title: 'Zadaci — ${widget.lectureName}',
        columns: [
          ColumnSpec<AssignmentDto>(
            label: 'Naslov',
            value: (item) => item.title,
          ),
          ColumnSpec<AssignmentDto>(
            label: 'Opis',
            value: (item) => truncate(item.description, 80),
          ),
          ColumnSpec<AssignmentDto>(
            label: 'Rok',
            value: (item) => formatDateTime(item.dueAt),
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<AssignmentProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'title': search,
        }),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          final provider = context.read<AssignmentProvider>();
          await provider.remove(item.id);
          return true;
        },
        extraActions: (context, item) => [
          IconButton(
            icon: const Icon(Icons.grading, size: 18),
            tooltip: 'Predaje',
            onPressed: () => _openSubmissions(item),
          ),
        ],
      ),
    );
  }
}
