import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_filter_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../../../widgets/store_profile_panel.dart';
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

  Future<void> _deleteStore() async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrdite brisanje',
      message: 'Da li ste sigurni da želite da obrišete ovu prodavnicu?',
    );
    if (confirmed != true) return;
    if (!mounted) return;

    try {
      await context.read<MusicStoreProvider>().remove(widget.storeId);
      if (!mounted) return;
      Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // No AppBar/title here — the store name is already shown right
          // below in the left panel, so a header up top would just repeat
          // it. Just enough to get back to the list.
          IconButton(
            icon: const Icon(Icons.arrow_back),
            tooltip: 'Nazad',
            onPressed: () => Navigator.of(context).pop(),
          ),
          Expanded(
            child: _isLoading
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
                                filterBar: EntityFilterDropdown<int?>(
                                  label: 'Tip instrumenta',
                                  width: 220,
                                  value: _instrumentTypeId,
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
                                groupKeyOf: _instrumentTypeId == null
                                    ? (i) => i.instrumentType.isNotEmpty
                                        ? i.instrumentType
                                        : 'Ostalo'
                                    : null,
                                fetcher: (page, pageSize, search) => context
                                    .read<StoreInstrumentProvider>()
                                    .search(pagedQuery(
                                  page,
                                  pageSize,
                                  search,
                                  searchField: 'search',
                                  filters: {
                                    'musicStoreId': widget.storeId,
                                    if (_instrumentTypeId != null)
                                      'instrumentTypeId': _instrumentTypeId,
                                  },
                                )),
                              ),
                            ),
                          ),
                        ],
                      ),
          ),
        ],
      ),
    );
  }

  Widget _buildLeftPanel() {
    final store = _store!;
    return StoreProfilePanel(
      store: store,
      showActions: true,
      onEdit: _openEdit,
      onDelete: _deleteStore,
    );
  }

}