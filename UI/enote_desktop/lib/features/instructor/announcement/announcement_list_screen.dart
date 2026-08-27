import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import 'announcement_form_screen.dart';
import 'announcement_provider.dart';

String _truncate(String text, int maxLength) {
  if (text.length <= maxLength) return text;
  return '${text.substring(0, maxLength)}…';
}

String _formatDate(DateTime d) {
  final day = d.day.toString().padLeft(2, '0');
  final month = d.month.toString().padLeft(2, '0');
  return '$day.$month.${d.year}.';
}

class AnnouncementListScreen extends StatefulWidget {
  final int courseId;
  final String courseName;

  const AnnouncementListScreen({
    super.key,
    required this.courseId,
    required this.courseName,
  });

  @override
  State<AnnouncementListScreen> createState() => _AnnouncementListScreenState();
}

class _AnnouncementListScreenState extends State<AnnouncementListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<AnnouncementDto>>();

  Future<void> _openForm([AnnouncementDto? existing]) async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => ChangeNotifierProvider<AnnouncementProvider>.value(
          value: context.read<AnnouncementProvider>(),
          child: AnnouncementFormScreen(
            courseId: widget.courseId,
            existing: existing,
          ),
        ),
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<AnnouncementDto>(
      key: _listKey,
      config: EntityListConfig<AnnouncementDto>(
        title: 'Objave — ${widget.courseName}',
        columns: [
          ColumnSpec<AnnouncementDto>(
            label: 'Naslov',
            value: (item) => item.title,
          ),
          ColumnSpec<AnnouncementDto>(
            label: 'Sadržaj',
            value: (item) => _truncate(item.content, 80),
          ),
          ColumnSpec<AnnouncementDto>(
            label: 'Datum objave',
            value: (item) => _formatDate(item.publishedAt),
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<AnnouncementProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
        }),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          final provider = context.read<AnnouncementProvider>();
          await provider.remove(item.id);
          return true;
        },
      ),
    );
  }
}
