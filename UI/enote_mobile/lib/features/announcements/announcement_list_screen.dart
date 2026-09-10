import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/paged_list_view.dart';
import 'announcement_provider.dart';

/// S25 — announcement list (tab 1 · Objave): paged list, search → `title`
/// server-side (01 A5, 02 §5 S25).
class AnnouncementListScreen extends StatefulWidget {
  const AnnouncementListScreen({super.key});

  @override
  State<AnnouncementListScreen> createState() => _AnnouncementListScreenState();
}

class _AnnouncementListScreenState extends State<AnnouncementListScreen>
    with AutomaticKeepAliveClientMixin {
  late final PagedFetchController<AnnouncementDto> _controller;
  Object? _error;

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<AnnouncementDto>(
      fetcher: _fetch,
      pageSize: 20,
      onError: (e) {
        if (mounted) setState(() => _error = e);
      },
    );
  }

  Future<PagedResult<AnnouncementDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) async {
    final result = await context.read<AnnouncementProvider>().getPage(
      params: {
        'page': page,
        'pageSize': pageSize,
        'includeTotalCount': true,
        if (search.isNotEmpty) 'title': search,
      },
    );
    if (mounted) setState(() => _error = null);
    return result;
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    return PagedListView<AnnouncementDto>(
      controller: _controller,
      error: _error,
      onRetry: () {
        setState(() => _error = null);
        _controller.refresh();
      },
      itemBuilder: (context, announcement) =>
          _AnnouncementRow(announcement: announcement),
    );
  }
}

class _AnnouncementRow extends StatelessWidget {
  final AnnouncementDto announcement;

  const _AnnouncementRow({required this.announcement});

  @override
  Widget build(BuildContext context) {
    final isCourse = announcement.scope == AnnouncementScope.course;
    return ListTile(
      leading: Icon(
        isCourse ? Icons.school_outlined : Icons.storefront_outlined,
      ),
      title: Text(announcement.title),
      subtitle: Text(
        '${orDash(announcement.courseName ?? announcement.storeName)} · '
        '${formatDate(announcement.publishedAt)}',
      ),
      trailing: Chip(label: Text(isCourse ? 'Kurs' : 'Prodavnica')),
      onTap: () => Navigator.of(context).pushNamed(
        AppRouter.announcementDetail,
        arguments: AnnouncementDetailArgs(announcement),
      ),
    );
  }
}
