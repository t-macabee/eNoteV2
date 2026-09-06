import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../../../widgets/pdf_report_button.dart';
import '../city/city_provider.dart';
import 'music_store_detail_screen.dart';
import 'music_store_form_screen.dart';
import 'music_store_provider.dart';

class MusicStoreListScreen extends StatefulWidget {
  const MusicStoreListScreen({super.key});

  @override
  State<MusicStoreListScreen> createState() => _MusicStoreListScreenState();
}

class _MusicStoreListScreenState extends State<MusicStoreListScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<MusicStoreDto>>();

  /// null = "Svi gradovi" (default) — no city filter applied.
  int? _cityId;
  List<CityDto> _cities = [];

  @override
  void initState() {
    super.initState();
    _loadCities();
  }

  Future<void> _loadCities() async {
    final result = await context.read<CityProvider>().search({
      'page': 1,
      'pageSize': 100,
      'includeTotalCount': true,
    });
    if (!mounted) return;
    setState(() => _cities = result.items);
  }

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
        onTap: (context, item) async {
          await Navigator.of(context).push(
            MaterialPageRoute(
              builder: (_) => MusicStoreDetailScreen(storeId: item.id),
            ),
          );
          _gridKey.currentState?.refresh();
        },
        filterBar: SizedBox(
          width: 220,
          child: DropdownButtonFormField<int?>(
            initialValue: _cityId,
            decoration: const InputDecoration(labelText: 'Grad'),
            items: [
              const DropdownMenuItem(value: null, child: Text('Svi gradovi')),
              ..._cities.map(
                (city) => DropdownMenuItem(value: city.id, child: Text(city.name)),
              ),
            ],
            onChanged: (cityId) {
              _cityId = cityId;
              _applyFilters();
            },
          ),
        ),
        // Read `_cityId` at call time (a field), not a snapshot — matches
        // UserGridScreen's rationale: setState doesn't rebuild
        // synchronously, so this closure must see the latest value even if
        // it runs against a stale build.
        fetcher: (page, pageSize, search) =>
            context.read<MusicStoreProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'storeName': search,
          if (_cityId != null) 'cityId': _cityId,
        }),
        onAdd: () => _openForm(),
        // Only group by city when no specific city filter is active — when
        // one city is selected every visible row already shares it, so a
        // section header would be redundant.
        groupKeyOf: _cityId == null
            ? (store) => store.addressCity ?? 'Nepoznat grad'
            : null,
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
