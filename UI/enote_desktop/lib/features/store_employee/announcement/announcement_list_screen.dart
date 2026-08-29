import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import 'announcement_form_screen.dart';
import 'announcement_provider.dart';

class AnnouncementListScreen extends StatefulWidget {
  const AnnouncementListScreen({super.key});

  @override
  State<AnnouncementListScreen> createState() => _AnnouncementListScreenState();
}

class _AnnouncementListScreenState extends State<AnnouncementListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<AnnouncementDto>>();

  Future<void> _openForm([AnnouncementDto? existing]) async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => AnnouncementFormScreen(
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _listKey.currentState?.refresh();
  }

  Widget _thumbnail(BuildContext context, String? imagePath) {
    const size = 40.0;
    if (imagePath == null || imagePath.trim().isEmpty) {
      return Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          color: Colors.grey.shade300,
          borderRadius: BorderRadius.circular(4),
        ),
        child: Icon(Icons.image, size: 20, color: Colors.grey.shade600),
      );
    }
    final trimmed = imagePath.trim();
    if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(4),
        child: Image.network(
          trimmed,
          width: size,
          height: size,
          fit: BoxFit.cover,
          errorBuilder: (_, _, _) => Container(
            width: size,
            height: size,
            color: Colors.grey.shade300,
            child: Icon(Icons.image, size: 20, color: Colors.grey.shade600),
          ),
        ),
      );
    }
    if (trimmed.startsWith('/')) {
      final client = context.read<ApiClient>();
      return ClipRRect(
        borderRadius: BorderRadius.circular(4),
        child: Image.network(
          '${client.baseUrl}$trimmed',
          headers: client.authHeaders,
          width: size,
          height: size,
          fit: BoxFit.cover,
          errorBuilder: (_, _, _) => Container(
            width: size,
            height: size,
            color: Colors.grey.shade300,
            child: Icon(Icons.image, size: 20, color: Colors.grey.shade600),
          ),
        ),
      );
    }
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: Colors.grey.shade300,
        borderRadius: BorderRadius.circular(4),
      ),
      child: Icon(Icons.image, size: 20, color: Colors.grey.shade600),
    );
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<AnnouncementDto>(
      key: _listKey,
      config: EntityListConfig<AnnouncementDto>(
        title: 'Objave',
        columns: [
          ColumnSpec<AnnouncementDto>(
            label: 'Naslov',
            value: (item) => item.title,
            cellBuilder: (context, item) => Row(
              children: [
                _thumbnail(context, item.imagePath),
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
            context.read<StoreAnnouncementProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
        }),
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          final provider = context.read<StoreAnnouncementProvider>();
          await provider.remove(item.id);
          return true;
        },
      ),
    );
  }
}
