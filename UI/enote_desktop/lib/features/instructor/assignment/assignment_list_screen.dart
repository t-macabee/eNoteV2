import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'assignment_form_screen.dart';
import 'assignment_provider.dart';



class AssignmentListScreen extends StatefulWidget {
  final int lectureId;
  final String lectureName;

  /// Defaults to [EntityListPresentation.page] (own Scaffold/AppBar, with a
  /// back/close button when pushed via Navigator). Pass
  /// [EntityListPresentation.embedded] only when the caller already
  /// provides that chrome.
  final EntityListPresentation presentation;
  final void Function(AssignmentDto assignment)? onOpenSubmissions;
  final VoidCallback? onMutation;

  const AssignmentListScreen({
    super.key,
    required this.lectureId,
    required this.lectureName,
    this.presentation = EntityListPresentation.page,
    this.onOpenSubmissions,
    this.onMutation,
  });

  @override
  State<AssignmentListScreen> createState() => _AssignmentListScreenState();
}

class _AssignmentListScreenState extends State<AssignmentListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<AssignmentDto>>();

  void _openSubmissions(AssignmentDto assignment) {
    widget.onOpenSubmissions?.call(assignment);
  }

  Future<void> _openForm([AssignmentDto? existing]) async {
    final provider = context.read<AssignmentProvider>();
    final saved = await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => ChangeNotifierProvider<AssignmentProvider>.value(
        value: provider,
        child: AssignmentFormScreen(
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
    return EntityListScreen<AssignmentDto>(
      key: _listKey,
      config: EntityListConfig<AssignmentDto>(
        title: 'Zadaci — ${widget.lectureName}',
        presentation: widget.presentation,
        listStyle: EntityListStyle.tiles,
        tileSubtitleColumns: const [2],
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
        fetcher: (page, pageSize, search) => context.read<AssignmentProvider>().search(pagedQuery(page, pageSize, search, searchField: 'title')),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          final provider = context.read<AssignmentProvider>();
          await provider.remove(item.id);
          widget.onMutation?.call();
          return true;
        },
        extraActions: (context, item) => [
          IconButton(
            icon: const Icon(Icons.grading, size: 18),
            tooltip: 'Predaje',
            onPressed: widget.onOpenSubmissions == null
                ? null
                : () => _openSubmissions(item),
          ),
        ],
      ),
    );
  }
}
