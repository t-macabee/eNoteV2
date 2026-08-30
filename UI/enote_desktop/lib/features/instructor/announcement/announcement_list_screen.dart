import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'announcement_form_screen.dart';
import 'announcement_provider.dart';

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
    final provider = context.read<AnnouncementProvider>();
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => ChangeNotifierProvider<AnnouncementProvider>.value(
        value: provider,
        child: AnnouncementFormScreen(
          courseId: widget.courseId,
          existing: existing,
          presentation: EntityFormPresentation.dialog,
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
            cellBuilder: (context, item) => Row(
              children: [
                ImageThumbnail(
                  imageUrl: item.imagePath,
                  apiClient: context.read<ApiClient>(),
                ),
                const SizedBox(width: 8),
                Text(item.title),
              ],
            ),
          ),
          ColumnSpec<AnnouncementDto>(
            label: 'Sadržaj',
            value: (item) => truncate(item.content, 80),
          ),
          ColumnSpec<AnnouncementDto>(
            label: 'Datum objave',
            value: (item) => formatDate(item.publishedAt),
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
