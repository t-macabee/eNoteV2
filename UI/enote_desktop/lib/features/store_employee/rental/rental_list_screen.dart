import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/pdf_report_button.dart';
import '../instrument/instrument_provider.dart';
import 'rental_detail_screen.dart';
import 'rental_provider.dart';
import 'rental_status_display.dart';

class RentalListScreen extends StatefulWidget {
  const RentalListScreen({super.key});

  @override
  State<RentalListScreen> createState() => _RentalListScreenState();
}

class _RentalListScreenState extends State<RentalListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<InstrumentRentalDto>>();
  InstrumentRentalStatus? _selectedStatus;
  int? _selectedInstrumentId;
  late Future<List<InstrumentDto>> _instrumentsFuture;

  @override
  void initState() {
    super.initState();
    _instrumentsFuture = context.read<InstrumentProvider>().search({
      'page': 1,
      'pageSize': 100,
      'includeTotalCount': false,
    }).then((result) => result.items);
  }

  Widget _buildFilterBar() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          Expanded(
            child: DropdownButtonFormField<InstrumentRentalStatus?>(
              initialValue: _selectedStatus,
              decoration: const InputDecoration(
                labelText: 'Status',
                border: OutlineInputBorder(),
              ),
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
          ),
          const SizedBox(width: 16),
          Expanded(
            child: FutureBuilder<List<InstrumentDto>>(
              future: _instrumentsFuture,
              builder: (context, snapshot) {
                final instruments = snapshot.data ?? const <InstrumentDto>[];
                return DropdownButtonFormField<int?>(
                  initialValue: _selectedInstrumentId,
                  decoration: const InputDecoration(
                    labelText: 'Instrument',
                    border: OutlineInputBorder(),
                  ),
                  items: [
                    const DropdownMenuItem(value: null, child: Text('Svi instrumenti')),
                    for (final instrument in instruments)
                      DropdownMenuItem(value: instrument.id, child: Text(instrument.model)),
                  ],
                  onChanged: snapshot.connectionState == ConnectionState.done
                      ? (value) {
                          setState(() => _selectedInstrumentId = value);
                          _listKey.currentState?.refresh();
                        }
                      : null,
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final apiClient = context.read<ApiClient>();

    return EntityListScreen<InstrumentRentalDto>(
      key: _listKey,
      config: EntityListConfig<InstrumentRentalDto>(
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
                '${item.fee.toStringAsFixed(2)} / ${item.totalFee?.toStringAsFixed(2) ?? '-'}',
          ),
        ],
        showSearchBar: false,
        filterBar: _buildFilterBar(),
        fetcher: (page, pageSize, search) =>
            context.read<RentalProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (_selectedStatus != null)
            'rentalStatus': _selectedStatus!.toJson(),
          if (_selectedInstrumentId != null)
            'instrumentId': _selectedInstrumentId,
        }),
        onEdit: (context, item) async {
          await Navigator.of(context).push<bool>(
            MaterialPageRoute<bool>(
              builder: (_) => RentalDetailScreen(rentalId: item.id),
            ),
          );
          _listKey.currentState?.refresh();
        },
        showAddButton: false,
        showDeleteConfirmation: false,
        trailing: const PdfReportButton(
          label: 'Izvještaj',
          fileName: 'store-rentals.pdf',
          endpoint: 'shop/rentals/report',
        ),
      ),
    );
  }

}
