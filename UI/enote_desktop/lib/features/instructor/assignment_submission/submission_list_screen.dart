import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_list_screen.dart';
import 'submission_provider.dart';

String _formatDateTime(DateTime d) {
  final day = d.day.toString().padLeft(2, '0');
  final month = d.month.toString().padLeft(2, '0');
  final hour = d.hour.toString().padLeft(2, '0');
  final minute = d.minute.toString().padLeft(2, '0');
  return '$day.$month.${d.year}. $hour:$minute';
}

class SubmissionListScreen extends StatefulWidget {
  final int lectureId;
  final int assignmentId;
  final String assignmentTitle;

  const SubmissionListScreen({
    super.key,
    required this.lectureId,
    required this.assignmentId,
    required this.assignmentTitle,
  });

  @override
  State<SubmissionListScreen> createState() => _SubmissionListScreenState();
}

class _SubmissionListScreenState extends State<SubmissionListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<AssignmentSubmissionDto>>();

  Future<void> _openGradeDialog(AssignmentSubmissionDto item) async {
    final controller = TextEditingController(
      text: item.grade?.toString() ?? '',
    );
    final formKey = GlobalKey<FormState>();
    bool isSaving = false;
    final provider = context.read<SubmissionProvider>();

    final savedGrade = await showDialog<int>(
      context: context,
      barrierDismissible: false,
      builder: (_) => StatefulBuilder(
        builder: (dialogCtx, setDialogState) => AlertDialog(
          title: Text(
            'Ocijeni — ${item.studentName ?? 'Student ${item.studentId}'}',
          ),
          content: Form(
            key: formKey,
            child: TextFormField(
              controller: controller,
              keyboardType: TextInputType.number,
              autofocus: true,
              decoration: const InputDecoration(
                labelText: 'Ocjena (0–100)',
                border: OutlineInputBorder(),
              ),
              validator: Validators.grade(0, 100),
            ),
          ),
          actions: [
            TextButton(
              onPressed: isSaving ? null : () => Navigator.of(dialogCtx).pop(),
              child: const Text('Otkaži'),
            ),
            ElevatedButton(
              onPressed: isSaving
                  ? null
                  : () async {
                      if (!(formKey.currentState?.validate() ?? false)) return;
                      final parsed = int.tryParse(controller.text.trim());
                      if (parsed == null) return;
                      setDialogState(() => isSaving = true);
                      try {
                        await provider.grade(item.id, parsed);
                        if (dialogCtx.mounted)
                          Navigator.of(dialogCtx).pop(parsed);
                      } catch (e) {
                        if (dialogCtx.mounted) {
                          ErrorBanner.show(dialogCtx, message: e.toString());
                        }
                        setDialogState(() => isSaving = false);
                      }
                    },
              child: isSaving
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Sačuvaj'),
            ),
          ],
        ),
      ),
    );

    controller.dispose();

    if (savedGrade != null && mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Ocjena sačuvana.')));
      _listKey.currentState?.refresh();
    }
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<AssignmentSubmissionDto>(
      key: _listKey,
      config: EntityListConfig<AssignmentSubmissionDto>(
        title: 'Predaje — ${widget.assignmentTitle}',
        showAddButton: false,
        searchHint: 'Pretraži...',
        columns: [
          ColumnSpec<AssignmentSubmissionDto>(
            label: 'Student',
            value: (item) => item.studentName ?? 'Student ${item.studentId}',
          ),
          ColumnSpec<AssignmentSubmissionDto>(
            label: 'Predato',
            value: (item) => item.submittedAt != null
                ? _formatDateTime(item.submittedAt!)
                : '-',
          ),
          ColumnSpec<AssignmentSubmissionDto>(
            label: 'Ocjena',
            value: (item) => item.grade?.toString() ?? 'Neocijenjeno',
            style: (item) => item.grade == null
                ? TextStyle(
                    color: Colors.grey.shade600,
                    fontStyle: FontStyle.italic,
                  )
                : null,
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<SubmissionProvider>().search({
              'page': page,
              'pageSize': pageSize,
              'includeTotalCount': true,
            }),
        onEdit: (ctx, item) => _openGradeDialog(item),
        onDelete: null,
      ),
    );
  }
}
