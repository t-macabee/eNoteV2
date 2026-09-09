import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../../../widgets/store_profile_panel.dart';
import '../instrument/instrument_detail_dialog.dart';
import '../instrument/instrument_form_screen.dart';
import '../instrument/instrument_provider.dart';
import '../instrument/shop_instrument_type_provider.dart';
import 'shop_store_provider.dart';

class ShopStoreScreen extends StatefulWidget {
  final MusicStoreDto? initialStore;

  const ShopStoreScreen({
    super.key,
    this.initialStore,
  });

  @override
  State<ShopStoreScreen> createState() => _ShopStoreScreenState();
}

class _ShopStoreScreenState extends State<ShopStoreScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<InstrumentDto>>();
  MusicStoreDto? _store;
  bool _isLoading = true;
  String? _errorMessage;
  int? _instrumentTypeId;
  List<InstrumentTypeDto> _instrumentTypes = [];

  @override
  void initState() {
    super.initState();
    if (widget.initialStore != null) {
      _store = widget.initialStore;
      _isLoading = false;
    } else {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        _loadStore();
      });
    }
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadInstrumentTypes();
    });
  }

  Future<void> _loadInstrumentTypes() async {
    try {
      final provider = context.read<ShopInstrumentTypeProvider>();
      final result = await provider.search(pagedQuery(1, 100, ''));
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
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final store = await context.read<ShopStoreProvider>().getOwnStore();
      if (mounted) {
        setState(() {
          _store = store;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = userMessage(e);
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _openInstrumentForm([InstrumentDto? existing]) async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => InstrumentFormScreen(
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _gridKey.currentState?.refresh();
  }

  Widget _buildLeftPanel(MusicStoreDto store) {
    return StoreProfilePanel(store: store);
  }

  @override
  Widget build(BuildContext context) {
    final storeProvider = context.watch<ShopStoreProvider>();
    // Build-local view of the store: provider data wins when present,
    // otherwise fall back to the last value loaded by _loadStore().
    // Never assigns to _store here — _loadStore() remains its only writer.
    final store = storeProvider.store ?? _store;

    return _buildBody(context, store);
  }

  Widget _buildBody(BuildContext context, MusicStoreDto? store) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(_errorMessage!,
                style: const TextStyle(color: AppTheme.error)),
            const SizedBox(height: 12),
            FilledButton.icon(
              onPressed: _loadStore,
              icon: const Icon(Icons.refresh),
              label: const Text('Pokušaj ponovo'),
            ),
          ],
        ),
      );
    }

    if (store == null) {
      return const Center(child: Text('Nema podataka o prodavnici.'));
    }

    return Row(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SizedBox(
          width: 280,
          child: _buildLeftPanel(store),
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
              showAddButton: true,
              addLabel: 'Dodaj instrument',
              onAdd: () => _openInstrumentForm(),
              onTap: (context, item) async {
                // Deletion is handled inside InstrumentDetailDialog (it owns
                // its own confirm/delete flow and calls InstrumentProvider
                // directly) — EntityGridConfig.onDelete has no UI trigger
                // here (no cardActions delete button), so it isn't set.
                final changed =
                    await InstrumentDetailDialog.show(context, item);
                if (changed == true) {
                  _gridKey.currentState?.refresh();
                }
              },
              filterBar: SizedBox(
                width: 220,
                child: DropdownButtonFormField<int?>(
                  isExpanded: true,
                  initialValue: _instrumentTypeId,
                  decoration:
                      const InputDecoration(labelText: 'Tip instrumenta'),
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
              fetcher: (page, pageSize, search) => context
                  .read<InstrumentProvider>()
                  .search(pagedQuery(
                    page,
                    pageSize,
                    search,
                    searchField: 'model',
                    filters: {
                      if (_instrumentTypeId != null)
                        'instrumentTypeId': _instrumentTypeId,
                    },
                  )),
            ),
          ),
        ),
      ],
    );
  }
}
