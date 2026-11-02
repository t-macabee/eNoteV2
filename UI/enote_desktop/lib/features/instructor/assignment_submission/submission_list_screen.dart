import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/dashboard_count.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/status_chip.dart';
import '../../../widgets/status_filter_tabs.dart';
import 'submission_provider.dart';

class SubmissionListScreen extends StatefulWidget {
  final int lectureId;
  final int assignmentId;
  final String assignmentTitle;

  final EntityListPresentation presentation;
  final VoidCallback? onMutation;

  const SubmissionListScreen({
    super.key,
    required this.lectureId,
    required this.assignmentId,
    required this.assignmentTitle,
    this.presentation = EntityListPresentation.page,
    this.onMutation,
  });

  @override
  State<SubmissionListScreen> createState() => _SubmissionListScreenState();
}

class _SubmissionListScreenState extends State<SubmissionListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<AssignmentSubmissionDto>>();

  final _ungradedCount = DashboardCount();
  bool? _isGraded = false;

  @override
  void initState() {
    super.initState();
    _loadUngradedCount();
  }

  @override
  void dispose() {
    _ungradedCount.dispose();
    super.dispose();
  }

  Future<void> _loadUngradedCount() async {
    if (!mounted) return;
    await _ungradedCount.load(
      () => context.read<SubmissionProvider>().countUngraded(),
    );
  }

  void _onGradedChanged(bool? value) {
    setState(() => _isGraded = value);
    _listKey.currentState?.refresh(resetPage: true);
  }

  Future<void> _openGradeDialog(AssignmentSubmissionDto item) async {
    final controller = TextEditingController(
      text: item.grade?.toString() ?? '',
    );
    final feedbackController = TextEditingController(text: item.feedback ?? '');
    final formKey = GlobalKey<FormState>();
    bool isSaving = false;
    final provider = context.read<SubmissionProvider>();

    final savedGrade = await showDialog<int>(
      context: context,
      barrierDismissible: false,
      builder: (_) => StatefulBuilder(
        builder: (dialogCtx, setDialogState) => AlertDialog(
          title: Text('Ocijeni — ${item.studentName ?? 'Nepoznat korisnik'}'),
          content: Form(
            key: formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: controller,
                  keyboardType: TextInputType.number,
                  autofocus: true,
                  decoration: const InputDecoration(
                    labelText: 'Ocjena (0–100)',
                    border: OutlineInputBorder(),
                  ),
                  validator: Validators.grade(0, 100),
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: feedbackController,
                  maxLines: 4,
                  maxLength: 1000,
                  decoration: const InputDecoration(
                    labelText: 'Komentar (nije obavezno)',
                    border: OutlineInputBorder(),
                  ),
                ),
              ],
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
                        await provider.grade(
                          item.id,
                          parsed,
                          feedback: feedbackController.text.trim(),
                        );
                        if (dialogCtx.mounted) {
                          Navigator.of(dialogCtx).pop(parsed);
                        }
                      } catch (e) {
                        if (dialogCtx.mounted) {
                          ErrorBanner.show(dialogCtx, message: userMessage(e));
                        }
                        setDialogState(() => isSaving = false);
                      }
                    },
              child: isSaving
                  ? const SizedBox(
                      width: AppSize.spinnerSm,
                      height: AppSize.spinnerSm,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Sačuvaj'),
            ),
          ],
        ),
      ),
    );

    controller.dispose();
    feedbackController.dispose();

    if (savedGrade != null && mounted) {
      widget.onMutation?.call();
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Ocjena sačuvana.')));
      _listKey.currentState?.refresh();
      await _loadUngradedCount();
    }
  }

  Widget _buildStatusTabs() {
    return ListenableBuilder(
      listenable: _ungradedCount,
      builder: (context, _) => StatusFilterTabs<bool?>(
        selected: _isGraded,
        options: [
          StatusFilterOption(
            label: 'Neocijenjeno',
            value: false,
            count: _ungradedCount.value,
          ),
          const StatusFilterOption(label: 'Ocijenjeno', value: true),
          const StatusFilterOption(label: 'Sve', value: null),
        ],
        onChanged: _onGradedChanged,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<AssignmentSubmissionDto>(
      key: _listKey,
      config: EntityListConfig<AssignmentSubmissionDto>(
        title: 'Predaje — ${widget.assignmentTitle}',
        presentation: widget.presentation,
        showAddButton: false,
        showSearchBar: true,
        searchHint: 'Pretraži po imenu studenta...',
        statusTabs: _buildStatusTabs(),
        columns: [
          ColumnSpec<AssignmentSubmissionDto>(
            label: 'Student',
            value: (item) => item.studentName ?? 'Nepoznat korisnik',
          ),
          ColumnSpec<AssignmentSubmissionDto>(
            label: 'Predato',
            value: (item) => item.submittedAt != null
                ? formatDateTime(item.submittedAt!)
                : '-',
          ),
          ColumnSpec<AssignmentSubmissionDto>(
            label: 'Ocjena',
            value: (item) => item.grade?.toString() ?? 'Neocijenjeno',
            cellBuilder: (context, item) => item.grade == null
                ? const StatusChip(
                    label: 'Neocijenjeno',
                    tone: StatusTone.warning,
                  )
                : Text(item.grade.toString()),
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<SubmissionProvider>().search(
              pagedQuery(
                page,
                pageSize,
                search,
                searchField: 'studentName',
                filters: {if (_isGraded != null) 'isGraded': _isGraded},
              ),
            ),
        onEdit: (ctx, item) => _openGradeDialog(item),
      ),
    );
  }
}
