import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/pdf_report_button.dart';
import '../ranking/ranking_screen.dart';

class CourseRankingView extends StatelessWidget {
  final int courseId;
  final String reportEndpoint;

  const CourseRankingView({
    super.key,
    required this.courseId,
    required this.reportEndpoint,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Row(
            children: [
              Text(
                'Rangiranje – 15 najboljih',
                style: Theme.of(context).textTheme.titleMedium
                    ?.copyWith(fontWeight: FontWeight.w600),
              ),
              const Spacer(),
              PdfReportButton(
                label: 'Izvještaj – ocijenjeni studenti',
                fileName: reportFileName('course-$courseId-ranking'),
                endpoint: reportEndpoint,
              ),
            ],
          ),
        ),
        Expanded(child: RankingView(courseId: courseId)),
      ],
    );
  }
}
