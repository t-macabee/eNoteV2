import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/async_state_view.dart';
import '../../widgets/detail_header.dart';
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
    final imagePath = announcement.imagePath;
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.only(bottom: AppSpacing.lg),
        children: [
          if (imagePath != null && imagePath.isNotEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                AppSpacing.lg,
                AppSpacing.lg,
                0,
              ),
              child: HeroImage(
                imageUrl: imagePath,
                apiClient: context.read<ApiClient>(),
              ),
            ),
          DetailHeader(
            title: announcement.title,
            subtitle: '$source · ${formatDateTime(announcement.publishedAt)}',
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
            child: Text(
              announcement.content,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          ),
        ],
      ),
    );
  }
}
