import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../../../widgets/pdf_report_button.dart';
import '../city/city_provider.dart';
import 'music_store_detail_dialog.dart';
import 'music_store_form_screen.dart';
import 'music_store_provider.dart';

class MusicStoreListScreen extends StatefulWidget {
  const MusicStoreListScreen({super.key});

  @override
  State<MusicStoreListScreen> createState() => _MusicStoreListScreenState();
}

class _MusicStoreListScreenState extends State<MusicStoreListScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<MusicStoreDto>>();

  int? _cityId;

  void _applyFilters() {
    setState(() {});
    _gridKey.currentState?.refresh(resetPage: true);
  }

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
        searchHint: 'Pretraži...',
        placeholderIcon: Icons.storefront_outlined,
        titleOf: (item) => item.storeName,
        imageUrlOf: (store) => store.imagePath,
        onTap: (context, item) async {
          await MusicStoreDetailDialog.show(context, item.id);
          _gridKey.currentState?.refresh();
        },
        filterBar: AsyncDropdown<CityDto>(
          label: 'Grad',
          width: 220,
          emptyOptionLabel: 'Svi gradovi',
          value: _cityId,
          fetcher: () => fetchAllPages(
            (query) => context.read<CityProvider>().search(query),
          ),
          itemLabel: (city) => city.name,
          itemId: (city) => city.id,
          onChanged: (cityId, _) {
            _cityId = cityId as int?;
            _applyFilters();
          },
        ),
        fetcher: (page, pageSize, search) => context.read<MusicStoreProvider>().search(pagedQuery(page, pageSize, search, searchField: 'storeName', filters: {
          if (_cityId != null) 'cityId': _cityId,
})),
        onAdd: () => _openForm(),
        groupKeyOf: _cityId == null
            ? (store) => store.addressCity ?? 'Nepoznat grad'
            : null,
        trailing: PdfReportButton(
          label: 'Izvještaj',
          fileName: reportFileName('music-stores-report'),
          endpoint: 'admin/music-stores/report',
        ),
      ),
    );
  }
}
