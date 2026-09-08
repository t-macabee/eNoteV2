import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/detail_row.dart';
import '../../../widgets/dialog_shell.dart';

/// Read-only detail dialog for a single announcement.
class AnnouncementDetailDialog extends StatelessWidget {
  final AnnouncementDto announcement;

  const AnnouncementDetailDialog({
    super.key,
    required this.announcement,
  });

  static Future<void> show(BuildContext context, AnnouncementDto announcement) {
    return showDialog<void>(
      context: context,
      builder: (_) => AnnouncementDetailDialog(announcement: announcement),
    );
  }

  @override
  Widget build(BuildContext context) {
    return DialogShell(
      title: announcement.title,
      width: DialogShellWidth.sm,
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            DetailRow(
              icon: Icons.title_outlined,
              label: 'Naslov',
              value: announcement.title.isNotEmpty ? announcement.title : '-',
            ),
            DetailRow(
              icon: Icons.description_outlined,
              label: 'Sadržaj',
              value: announcement.content.isNotEmpty ? announcement.content : '-',
            ),
            DetailRow(
              icon: Icons.event_outlined,
              label: 'Datum objave',
              value: formatDate(announcement.publishedAt),
            ),
          ],
        ),
      ),
    );
  }
}
