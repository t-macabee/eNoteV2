import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'announcement_form_screen.dart';

class AnnouncementListScreen extends StatefulWidget {
  final BaseProvider<AnnouncementDto> provider;
  final String? title;

  const AnnouncementListScreen({
    super.key,
    required this.provider,
    this.title,
  });

  @override
  State<AnnouncementListScreen> createState() => _AnnouncementListScreenState();
}

class _AnnouncementListScreenState extends State<AnnouncementListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<AnnouncementDto>>();

  Future<void> _openForm([AnnouncementDto? existing]) async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => AnnouncementFormScreen(
        provider: widget.provider,
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<AnnouncementDto>(
      key: _listKey,
      config: EntityListConfig<AnnouncementDto>(
        title: widget.title ?? 'Objave',
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
        fetcher: (page, pageSize, search) => widget.provider.search(pagedQuery(page, pageSize, search)),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          await widget.provider.remove(item.id);
          return true;
        },
      ),
    );
  }
}
