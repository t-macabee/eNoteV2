import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/async_dropdown.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/detail_row.dart';
import '../../../widgets/entity_detail_page.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_form_sections.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../instrument_type/instrument_type_provider.dart';
import 'music_store_form_screen.dart';
import 'music_store_provider.dart';
import 'store_instrument_provider.dart';

class MusicStoreDetailScreen extends StatefulWidget {
  final int storeId;

  const MusicStoreDetailScreen({super.key, required this.storeId});

  static Future<void> open(BuildContext context, int storeId) {
    return Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => MusicStoreDetailScreen(storeId: storeId),
      ),
    );
  }

  @override
  State<MusicStoreDetailScreen> createState() => _MusicStoreDetailScreenState();
}

class _MusicStoreDetailScreenState extends State<MusicStoreDetailScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<InstrumentDto>>();
  MusicStoreDto? _store;
  bool _isLoading = true;
  int? _instrumentTypeId;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadStore();
    });
  }

  void _applyFilters() {
    setState(() {});
    _gridKey.currentState?.refresh(resetPage: true);
  }

  Future<void> _loadStore() async {
    setState(() => _isLoading = true);
    try {
      final store = await context.read<MusicStoreProvider>().getById(
        widget.storeId,
      );
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
      message:
          'Da li ste sigurni da želite da obrišete prodavnicu "${_store?.storeName ?? ''}"?',
    );
    if (confirmed != true) return;
    if (!mounted) return;

    try {
      await context.read<MusicStoreProvider>().remove(widget.storeId);
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Prodavnica je obrisana.')));
      Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading || _store == null) {
      return Scaffold(
        appBar: AppBar(
          leading: IconButton(
            icon: const Icon(Icons.close),
            tooltip: 'Zatvori',
            onPressed: () => Navigator.of(context).pop(),
          ),
          title: Text(
            _store?.storeName ?? 'Prodavnica',
            overflow: TextOverflow.ellipsis,
          ),
        ),
        body: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : Center(
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
              ),
      );
    }

    final store = _store!;
    return EntityDetailPage(
      title: store.storeName,
      onClose: () => Navigator.of(context).pop(),
      actions: [
        TextButton.icon(
          onPressed: _openEdit,
          icon: const Icon(Icons.edit_outlined),
          label: const Text('Uredi'),
        ),
        TextButton.icon(
          onPressed: _deleteStore,
          icon: const Icon(Icons.delete_outline),
          label: const Text('Obriši'),
          style: TextButton.styleFrom(foregroundColor: AppTheme.error),
        ),
        const SizedBox(width: AppSpacing.sm),
      ],
      tabs: [
        EntityDetailTab(
          label: 'Pregled',
          icon: Icons.storefront_outlined,
          builder: (_) => _StoreOverview(store: store),
        ),
        EntityDetailTab(
          label: 'Instrumenti',
          icon: Icons.music_note,
          builder: (_) => EntityGridScreen<InstrumentDto>(
            key: _gridKey,
            config: EntityGridConfig<InstrumentDto>(
              embedded: true,
              searchHint: 'Pretraži instrumente...',
              placeholderIcon: Icons.music_note,
              titleOf: (i) => i.model,
              subtitleOf: (i) => i.manufacturer,
              imageUrlOf: (i) => i.imagePath,
              showAddButton: false,
              filterBar: AsyncDropdown<InstrumentTypeDto>(
                label: 'Tip instrumenta',
                width: 220,
                emptyOptionLabel: 'Svi instrumenti',
                value: _instrumentTypeId,
                fetcher: () => fetchAllPages(
                  (query) =>
                      context.read<InstrumentTypeProvider>().search(query),
                ),
                itemLabel: (type) => type.type,
                itemId: (type) => type.id,
                onChanged: (typeId, _) {
                  _instrumentTypeId = typeId as int?;
                  _applyFilters();
                },
              ),
              groupKeyOf: _instrumentTypeId == null
                  ? (i) => i.instrumentType.isNotEmpty
                        ? i.instrumentType
                        : 'Ostalo'
                  : null,
              fetcher: (page, pageSize, search) =>
                  context.read<StoreInstrumentProvider>().fetchPage(
                    page,
                    pageSize,
                    search,
                    musicStoreId: widget.storeId,
                    instrumentTypeId: _instrumentTypeId,
                  ),
            ),
          ),
        ),
      ],
    );
  }
}

({String address, String phone, String hours}) _storeOverviewText(
  MusicStoreDto store,
) {
  String address = '-';
  if (store.addressStreet != null && store.addressStreet!.isNotEmpty) {
    if (store.addressCity != null && store.addressCity!.isNotEmpty) {
      address = '${store.addressStreet}, ${store.addressCity}';
    } else {
      address = store.addressStreet!;
    }
  } else if (store.addressCity != null && store.addressCity!.isNotEmpty) {
    address = store.addressCity!;
  }

  final phone = (store.phoneNumber != null && store.phoneNumber!.isNotEmpty)
      ? store.phoneNumber!
      : '-';

  final hours = store.businessHours.isNotEmpty ? store.businessHours : '-';

  return (address: address, phone: phone, hours: hours);
}

class _StoreOverview extends StatelessWidget {
  final MusicStoreDto store;

  const _StoreOverview({required this.store});

  @override
  Widget build(BuildContext context) {
    final apiClient = context.read<ApiClient>();
    final texts = _storeOverviewText(store);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.xl),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final image = SizedBox(
            width: 320,
            child: AspectRatio(
              aspectRatio: 1,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(AppRadius.lg),
                child: networkImageOrPlaceholder(
                  store.imagePath,
                  apiClient,
                  size: double.infinity,
                  borderRadius: AppRadius.lg,
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
          );

          final section = FormSection(
            title: 'Podaci o prodavnici',
            children: [
              DetailRow(
                icon: Icons.location_on_outlined,
                label: 'Adresa',
                value: texts.address,
              ),
              DetailRow(
                icon: Icons.phone_outlined,
                label: 'Telefon',
                value: texts.phone,
              ),
              DetailRow(
                icon: Icons.access_time_outlined,
                label: 'Radno vrijeme',
                value: texts.hours,
              ),
            ],
          );

          if (constraints.maxWidth >= 800) {
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                image,
                const SizedBox(width: AppSpacing.xl),
                Expanded(child: section),
              ],
            );
          }

          return Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              image,
              const SizedBox(height: AppSpacing.xl),
              section,
            ],
          );
        },
      ),
    );
  }
}
