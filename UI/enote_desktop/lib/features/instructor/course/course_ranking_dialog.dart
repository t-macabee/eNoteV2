import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/dialog_shell.dart';
import '../../../widgets/pdf_report_button.dart';
import '../ranking/ranking_screen.dart';

class CourseRankingDialog extends StatelessWidget {
  final int courseId;
  final String reportEndpoint;

  const CourseRankingDialog({
    super.key,
    required this.courseId,
    required this.reportEndpoint,
  });

  static Future<void> show(
    BuildContext context, {
    required int courseId,
    required String reportEndpoint,
  }) {
    return showDialog<void>(
      context: context,
      builder: (_) => CourseRankingDialog(
        courseId: courseId,
        reportEndpoint: reportEndpoint,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return DialogShell(
      width: DialogShellWidth.lg,
      header: DialogShellHeader(
        label: const Text(
          'Rangiranje – 15 najboljih',
          style: TextStyle(
            color: AppTheme.textPrimary,
            fontWeight: FontWeight.w600,
            fontSize: 16,
          ),
        ),
        action: PdfReportButton(
          label: 'Izvještaj – svi studenti',
          fileName: 'course-ranking.pdf',
          endpoint: reportEndpoint,
        ),
        onClose: () => Navigator.of(context).pop(),
      ),
      body: RankingView(courseId: courseId),
    );
  }
}
