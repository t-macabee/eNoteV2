import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/pdf_report_button.dart';
import '../instrument/instrument_provider.dart';
import 'rental_detail_screen.dart';
import 'rental_provider.dart';

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
                  DropdownMenuItem(value: status, child: Text(_statusLabel(status))),
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
        title: 'Zahtjevi za iznajmljivanje',
        columns: [
          ColumnSpec<InstrumentRentalDto>(
            label: 'Instrument',
            value: (item) => item.instrumentModel,
          ),
          ColumnSpec<InstrumentRentalDto>(
            label: 'Student',
            value: (item) => item.studentName ?? 'Student #${item.studentUserId}',
          ),
          ColumnSpec<InstrumentRentalDto>(
            label: 'Status',
            value: (item) => _statusLabel(item.rentalStatus),
            style: (item) => TextStyle(
              color: _statusColor(item.rentalStatus),
              fontWeight: FontWeight.w600,
            ),
          ),
          ColumnSpec<InstrumentRentalDto>(
            label: 'Zatraženo',
            value: (item) =>
                '${item.requestedAt.day}.${item.requestedAt.month}.${item.requestedAt.year}.',
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
        trailing: PdfReportButton(
          label: 'Izvještaj',
          fileName: 'store-rentals.pdf',
          fetchPdf: () async {
            final response = await apiClient.get('shop/rentals/report');
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

  static String _statusLabel(InstrumentRentalStatus status) => switch (status) {
        InstrumentRentalStatus.pending => 'Na čekanju',
        InstrumentRentalStatus.approved => 'Odobreno',
        InstrumentRentalStatus.active => 'Aktivno',
        InstrumentRentalStatus.completed => 'Završeno',
        InstrumentRentalStatus.rejected => 'Odbijeno',
        InstrumentRentalStatus.canceled => 'Otkazano',
        InstrumentRentalStatus.returnedEarly => 'Prijevremeni povrat',
      };

  static Color _statusColor(InstrumentRentalStatus status) => switch (status) {
        InstrumentRentalStatus.pending => Colors.orange,
        InstrumentRentalStatus.approved => Colors.blue,
        InstrumentRentalStatus.active => Colors.green,
        InstrumentRentalStatus.completed => Colors.grey,
        InstrumentRentalStatus.rejected => Colors.red,
        InstrumentRentalStatus.canceled => Colors.red.shade300,
        InstrumentRentalStatus.returnedEarly => Colors.purple,
      };
}
