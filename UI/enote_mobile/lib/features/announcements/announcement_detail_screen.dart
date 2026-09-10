import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

/// S26 — announcement detail (02 §5 S26): the passed object is already
/// loaded, so no request is issued. Names and dates only, never ids.
class AnnouncementDetailScreen extends StatelessWidget {
  final AnnouncementDto announcement;

  const AnnouncementDetailScreen({super.key, required this.announcement});

  @override
  Widget build(BuildContext context) {
    final isCourse = announcement.scope == AnnouncementScope.course;
    final source = isCourse
        ? 'Kurs · ${orDash(announcement.courseName)}'
        : 'Prodavnica · ${orDash(announcement.storeName)}';
    return Scaffold(
      appBar: AppBar(title: const Text('Objava')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            announcement.title,
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 8),
          Text(
            source,
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 4),
          Text(
            formatDateTime(announcement.publishedAt),
            style: Theme.of(context).textTheme.labelMedium,
          ),
          const SizedBox(height: 12),
          Text(
            announcement.content,
            style: Theme.of(context).textTheme.bodyMedium,
          ),
        ],
      ),
    );
  }
}
