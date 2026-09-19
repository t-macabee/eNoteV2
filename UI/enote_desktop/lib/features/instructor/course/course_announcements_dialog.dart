import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/dialog_shell.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../shared/announcement/announcement_list_screen.dart';
import '../../shared/announcement/announcement_provider.dart';

class CourseAnnouncementsDialog extends StatelessWidget {
  final int courseId;
  final ApiClient apiClient;

  const CourseAnnouncementsDialog({
    super.key,
    required this.courseId,
    required this.apiClient,
  });

  static Future<void> show(
    BuildContext context, {
    required int courseId,
    required ApiClient apiClient,
  }) {
    return showDialog<void>(
      context: context,
      builder: (_) => CourseAnnouncementsDialog(
        courseId: courseId,
        apiClient: apiClient,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return DialogShell(
      width: DialogShellWidth.lg,
      header: DialogShellHeader(
        label: const Text(
          'Objave',
          style: TextStyle(
            color: AppTheme.textPrimary,
            fontWeight: FontWeight.w600,
            fontSize: 16,
          ),
        ),
        onClose: () => Navigator.of(context).pop(),
      ),
      body: ChangeNotifierProvider<AnnouncementProvider>(
        create: (_) => AnnouncementProvider(
          apiClient: apiClient,
          courseId: courseId,
        ),
        builder: (ctx, _) => AnnouncementListScreen(
          provider: ctx.read<AnnouncementProvider>(),
          presentation: EntityListPresentation.embedded,
        ),
      ),
    );
  }
}
