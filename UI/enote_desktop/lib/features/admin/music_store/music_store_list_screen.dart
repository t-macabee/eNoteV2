import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../../../widgets/pdf_report_button.dart';
import 'music_store_form_screen.dart';
import 'music_store_provider.dart';

class MusicStoreListScreen extends StatefulWidget {
  const MusicStoreListScreen({super.key});

  @override
  State<MusicStoreListScreen> createState() => _MusicStoreListScreenState();
}

class _MusicStoreListScreenState extends State<MusicStoreListScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<MusicStoreDto>>();

  Future<void> _openForm([MusicStoreDto? existing]) async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => MusicStoreFormScreen(
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _gridKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityGridScreen<MusicStoreDto>(
      key: _gridKey,
      config: EntityGridConfig<MusicStoreDto>(
        title: 'Muzičke prodavnice',
        searchHint: 'Pretraži...',
        placeholderIcon: Icons.storefront_outlined,
        titleOf: (item) => item.storeName,
        subtitleOf: (item) => item.businessHours,
        aboveGrid: const SizedBox(height: 12),
        onTap: (context, item) => _openForm(item),
        onDelete: (context, item) async {
          await context.read<MusicStoreProvider>().remove(item.id);
          return true;
        },
        fetcher: (page, pageSize, search) =>
            context.read<MusicStoreProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'storeName': search,
        }),
        onAdd: () => _openForm(),
        trailing: PdfReportButton(
          label: 'Izvještaj',
          fileName: 'music-stores-report.pdf',
          fetchPdf: () async {
            final apiClient = context.read<ApiClient>();
            final response = await apiClient.get('admin/music-stores/report');
            if (response.statusCode >= 400) {
              throw ApiException(
                ApiErrorMapper.mapError(response.statusCode, response.body),
              );
            }
            return Uint8List.fromList(response.bodyBytes);
          },
        ),
      ),
    );
  }
}
