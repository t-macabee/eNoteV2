import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/async_state_view.dart';
import '../../widgets/wide_image.dart';
import 'announcement_provider.dart';

class AnnouncementDetailScreen extends StatefulWidget {
  final int announcementId;

  const AnnouncementDetailScreen({super.key, required this.announcementId});

  @override
  State<AnnouncementDetailScreen> createState() =>
      _AnnouncementDetailScreenState();
}

class _AnnouncementDetailScreenState extends State<AnnouncementDetailScreen> {
  AnnouncementDto? _announcement;
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final announcement = await context.read<AnnouncementProvider>().getById(
        widget.announcementId,
      );
      if (mounted) {
        setState(() {
          _announcement = announcement;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e;
          _loading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final announcement = _announcement;
    return Scaffold(
      appBar: AppBar(title: const Text('Objava')),
      body: AsyncStateView(
        isLoading: _loading && announcement == null,
        error: announcement == null ? _error : null,
        onRetry: _load,
        child: announcement == null
            ? const SizedBox.shrink()
            : _body(announcement),
      ),
    );
  }

  Widget _body(AnnouncementDto announcement) {
    final isCourse = announcement.scope == AnnouncementScope.course;
    final source = isCourse
        ? 'Kurs · ${orDash(announcement.courseName)}'
        : 'Prodavnica · ${orDash(announcement.storeName)}';
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (announcement.imagePath != null &&
            announcement.imagePath!.isNotEmpty) ...[
          HeroImage(
            imageUrl: announcement.imagePath,
            apiClient: context.read<ApiClient>(),
          ),
          const SizedBox(height: 16),
        ],
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
    );
  }
}
