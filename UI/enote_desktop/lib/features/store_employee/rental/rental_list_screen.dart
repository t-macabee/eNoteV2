import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_filter_dropdown.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/pdf_report_button.dart';
import '../instrument/instrument_provider.dart';
import 'rental_detail_dialog.dart';
import 'rental_provider.dart';
import 'rental_status_display.dart';

class RentalListScreen extends StatefulWidget {
  final EntityListPresentation presentation;

  const RentalListScreen({
    super.key,
    this.presentation = EntityListPresentation.embedded,
  });

  @override
  State<RentalListScreen> createState() => _RentalListScreenState();
}

class _RentalListScreenState extends State<RentalListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<InstrumentRentalDto>>();
  InstrumentRentalStatus? _selectedStatus;
  int? _selectedInstrumentId;
  List<InstrumentDto> _instruments = [];
  bool _instrumentsLoading = true;
  String? _instrumentsError;

  @override
  void initState() {
    super.initState();
    _loadInstruments();
  }

  Future<void> _loadInstruments() async {
    setState(() {
      _instrumentsLoading = true;
      _instrumentsError = null;
    });
    try {
      final result = await context
          .read<InstrumentProvider>()
          .search(pagedQuery(1, 100, ''));
      if (!mounted) return;
      setState(() {
        _instruments = result.items;
        _instrumentsLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _instrumentsError = 'Filteri nisu učitani.';
        _instrumentsLoading = false;
      });
    }
  }

  Widget _buildFilterBar() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          EntityFilterDropdown<InstrumentRentalStatus?>(
            label: 'Status',
            width: 220,
            outlined: true,
            value: _selectedStatus,
            items: [
              const DropdownMenuItem(value: null, child: Text('Svi statusi')),
              for (final status in InstrumentRentalStatus.values)
                DropdownMenuItem(value: status, child: Text(rentalStatusLabel(status))),
            ],
            onChanged: (value) {
              setState(() => _selectedStatus = value);
              _listKey.currentState?.refresh();
            },
          ),
          const SizedBox(width: 16),
          EntityFilterDropdown<int?>(
            label: 'Instrument',
            width: 220,
            outlined: true,
            value: _selectedInstrumentId,
            isLoading: _instrumentsLoading,
            errorText: _instrumentsError,
            onRetry: _loadInstruments,
            items: [
              const DropdownMenuItem(value: null, child: Text('Svi instrumenti')),
              for (final instrument in _instruments)
                DropdownMenuItem(
                  value: instrument.id,
                  child: Text(instrument.model),
                ),
            ],
            onChanged: (value) {
              setState(() => _selectedInstrumentId = value);
              _listKey.currentState?.refresh();
            },
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<InstrumentRentalDto>(
      key: _listKey,
      config: EntityListConfig<InstrumentRentalDto>(
        presentation: widget.presentation,
        columns: [
          ColumnSpec<InstrumentRentalDto>(
            label: 'Instrument',
            value: (item) => item.instrumentModel,
            cellBuilder: (context, item) => Row(
              children: [
                ImageThumbnail(
                  imageUrl: item.instrumentImagePath,
                  apiClient: context.read<ApiClient>(),
                ),
                const SizedBox(width: 8),
                Text(item.instrumentModel),
              ],
            ),
          ),
          ColumnSpec<InstrumentRentalDto>(
            label: 'Student',
            value: (item) => item.studentName ?? 'Nepoznat korisnik',
          ),
          ColumnSpec<InstrumentRentalDto>(
            label: 'Status',
            value: (item) => rentalStatusLabel(item.rentalStatus),
            style: (item) => TextStyle(
              color: rentalStatusColor(item.rentalStatus),
              fontWeight: FontWeight.w600,
            ),
          ),
          ColumnSpec<InstrumentRentalDto>(
            label: 'Zatraženo',
            value: (item) => formatDate(item.requestedAt),
          ),
          ColumnSpec<InstrumentRentalDto>(
            label: 'Naknada / Ukupno',
            value: (item) =>
                '${formatKM(item.fee)} / ${formatKM(item.totalFee)}',
          ),
        ],
        showSearchBar: false,
        filterBar: _buildFilterBar(),
        fetcher: (page, pageSize, search) => context.read<RentalProvider>().search(pagedQuery(page, pageSize, search, filters: {
          if (_selectedStatus != null)
            'rentalStatus': _selectedStatus!.toJson(),
          if (_selectedInstrumentId != null)
            'instrumentId': _selectedInstrumentId,
})),
        onEdit: (context, item) async {
          await RentalDetailDialog.show(context, item.id);
          _listKey.currentState?.refresh();
        },
        showAddButton: false,
        showDeleteConfirmation: false,
        trailing: PdfReportButton(
          label: 'Izvještaj',
          fileName: reportFileName('store-rentals'),
          endpoint: 'shop/rentals/report',
        ),
      ),
    );
  }

}
