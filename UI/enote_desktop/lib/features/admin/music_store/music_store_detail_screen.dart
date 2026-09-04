import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../instrument_type/instrument_type_provider.dart';
import 'music_store_form_screen.dart';
import 'music_store_provider.dart';
import 'store_instrument_provider.dart';

class MusicStoreDetailScreen extends StatefulWidget {
  final int storeId;

  const MusicStoreDetailScreen({super.key, required this.storeId});

  @override
  State<MusicStoreDetailScreen> createState() => _MusicStoreDetailScreenState();
}

class _MusicStoreDetailScreenState extends State<MusicStoreDetailScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<InstrumentDto>>();
  MusicStoreDto? _store;
  bool _isLoading = true;
  int? _instrumentTypeId;
  List<InstrumentTypeDto> _instrumentTypes = [];

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadStore();
      _loadInstrumentTypes();
    });
  }

  Future<void> _loadInstrumentTypes() async {
    try {
      final result = await context.read<InstrumentTypeProvider>().search({
        'page': 1,
        'pageSize': 100,
        'includeTotalCount': false,
      });
      if (!mounted) return;
      setState(() {
        _instrumentTypes = result.items;
      });
    } catch (_) {
      // Non-fatal if instrument types fail to load
    }
  }

  void _applyFilters() {
    setState(() {});
    _gridKey.currentState?.refresh(resetPage: true);
  }

  Future<void> _loadStore() async {
    setState(() => _isLoading = true);
    try {
      final store =
          await context.read<MusicStoreProvider>().getById(widget.storeId);
      if (!mounted) return;
      setState(() {
        _store = store;
      });
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _openEdit() async {
    if (_store == null) return;
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => MusicStoreFormScreen(
        existing: _store,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    if (!mounted) return;
    await _loadStore();
    _gridKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_store?.storeName.isNotEmpty == true
            ? _store!.storeName
            : 'Detalji prodavnice'),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _store == null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Text('Greška pri učitavanju prodavnice.'),
                      const SizedBox(height: 16),
                      ElevatedButton(
                        onPressed: _loadStore,
                        child: const Text('Pokušaj ponovo'),
                      ),
                    ],
                  ),
                )
              : Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    SizedBox(
                      width: 280,
                      child: _buildLeftPanel(),
                    ),
                    const VerticalDivider(width: 1),
                    Expanded(
                      child: EntityGridScreen<InstrumentDto>(
                        key: _gridKey,
                        config: EntityGridConfig<InstrumentDto>(
                          embedded: true,
                          searchHint: 'Pretraži instrumente...',
                          placeholderIcon: Icons.music_note,
                          titleOf: (i) => i.model,
                          subtitleOf: (i) => i.manufacturer,
                          imageUrlOf: (i) => i.imagePath,
                          showAddButton: false,
                          filterBar: SizedBox(
                            width: 220,
                            child: DropdownButtonFormField<int?>(
                              isExpanded: true,
                              initialValue: _instrumentTypeId,
                              decoration: const InputDecoration(labelText: 'Tip instrumenta'),
                              items: [
                                const DropdownMenuItem(
                                  value: null,
                                  child: Text('Svi instrumenti'),
                                ),
                                ..._instrumentTypes.map(
                                  (type) => DropdownMenuItem(
                                    value: type.id,
                                    child: Text(type.type),
                                  ),
                                ),
                              ],
                              onChanged: (typeId) {
                                _instrumentTypeId = typeId;
                                _applyFilters();
                              },
                            ),
                          ),
                          groupKeyOf: _instrumentTypeId == null
                              ? (i) => i.instrumentType.isNotEmpty
                                  ? i.instrumentType
                                  : 'Ostalo'
                              : null,
                          fetcher: (page, pageSize, search) =>
                              context.read<StoreInstrumentProvider>().search({
                            'page': page,
                            'pageSize': pageSize,
                            'includeTotalCount': true,
                            'musicStoreId': widget.storeId,
                            if (_instrumentTypeId != null)
                              'instrumentTypeId': _instrumentTypeId,
                            if (search.isNotEmpty) 'search': search,
                          }),
                        ),
                      ),
                    ),
                  ],
                ),
    );
  }

  Widget _buildLeftPanel() {
    final store = _store!;
    final apiClient = context.read<ApiClient>();

    String addressText = '-';
    if (store.addressStreet != null && store.addressStreet!.isNotEmpty) {
      if (store.addressCity != null && store.addressCity!.isNotEmpty) {
        addressText = '${store.addressStreet}, ${store.addressCity}';
      } else {
        addressText = store.addressStreet!;
      }
    } else if (store.addressCity != null && store.addressCity!.isNotEmpty) {
      addressText = store.addressCity!;
    }

    final phoneText =
        (store.phoneNumber != null && store.phoneNumber!.isNotEmpty)
            ? store.phoneNumber!
            : '-';

    final workHoursText =
        store.businessHours.isNotEmpty ? store.businessHours : '-';

    return Container(
      color: AppTheme.surfaceContainer,
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            AspectRatio(
              aspectRatio: 1.2,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(12),
                child: networkImageOrPlaceholder(
                  store.imagePath,
                  apiClient,
                  size: double.infinity,
                  borderRadius: 12,
                  placeholder: () => Container(
                    color: AppTheme.background,
                    child: const Center(
                      child: Icon(
                        Icons.storefront_outlined,
                        size: 48,
                        color: AppTheme.textTertiary,
                      ),
                    ),
                  ),
                ),
              ),
            ),
            const SizedBox(height: 16),
            Text(
              store.storeName,
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 16),
            const Divider(),
            const SizedBox(height: 8),
            _buildInfoRow(
              Icons.location_on_outlined,
              'Adresa',
              addressText,
            ),
            _buildInfoRow(
              Icons.phone_outlined,
              'Telefon',
              phoneText,
            ),
            _buildInfoRow(
              Icons.access_time_outlined,
              'Radno vrijeme',
              workHoursText,
            ),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: _openEdit,
                icon: const Icon(Icons.edit_outlined, size: 18),
                label: const Text('Uredi'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: AppTheme.textSecondary),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppTheme.textTertiary,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  style: const TextStyle(
                    fontSize: 14,
                    color: AppTheme.textPrimary,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
