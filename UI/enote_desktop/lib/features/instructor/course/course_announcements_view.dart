import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_list_screen.dart';
import '../../shared/announcement/announcement_list_screen.dart';
import '../../shared/announcement/announcement_provider.dart';

class CourseAnnouncementsView extends StatelessWidget {
  final int courseId;
  final ApiClient apiClient;

  const CourseAnnouncementsView({
    super.key,
    required this.courseId,
    required this.apiClient,
  });

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<AnnouncementProvider>(
      create: (_) =>
          AnnouncementProvider(apiClient: apiClient, courseId: courseId),
      builder: (ctx, _) => AnnouncementListScreen(
        provider: ctx.read<AnnouncementProvider>(),
        presentation: EntityListPresentation.embedded,
      ),
    );
  }
}
