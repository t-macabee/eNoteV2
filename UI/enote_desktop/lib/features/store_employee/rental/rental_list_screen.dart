import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/async_dropdown.dart';
import '../../../widgets/dashboard_count.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../../widgets/pdf_report_button.dart';
import '../../../widgets/status_chip.dart';
import '../../../widgets/status_filter_tabs.dart';
import '../../../widgets/workflow_actions.dart';
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
  static const double _instrumentFilterWidth = 220;

  final _listKey = GlobalKey<EntityListScreenState<InstrumentRentalDto>>();
  final _pendingCount = DashboardCount();
  InstrumentRentalStatus? _selectedStatus = InstrumentRentalStatus.pending;
  int? _selectedInstrumentId;

  @override
  void initState() {
    super.initState();
    _loadPendingCount();
  }

  @override
  void dispose() {
    _pendingCount.dispose();
    super.dispose();
  }

  Future<void> _loadPendingCount() async {
    if (!mounted) return;
    await _pendingCount.load(
      () => context.read<RentalProvider>().countPending(
        instrumentId: _selectedInstrumentId,
      ),
    );
  }

  String _rentalStatusText(InstrumentRentalDto item) => item.isOverdue
      ? '${rentalStatusLabel(item.rentalStatus)} · kasni ${item.overdueDays} dana'
      : rentalStatusLabel(item.rentalStatus);

  void _onStatusChanged(InstrumentRentalStatus? value) {
    setState(() => _selectedStatus = value);
    _listKey.currentState?.refresh(resetPage: true);
  }

  void _onInstrumentChanged(int? value) {
    setState(() => _selectedInstrumentId = value);
    _listKey.currentState?.refresh(resetPage: true);
    _loadPendingCount();
  }

  Future<void> _runTransition(
    InstrumentRentalDto item,
    RentalTrigger trigger, {
    String? note,
  }) async {
    await context.read<RentalProvider>().transition(
      item.id,
      trigger,
      note: note,
    );

    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(rentalTransitionSuccessMessage(trigger))),
    );
    _listKey.currentState?.refresh();
    await _loadPendingCount();
  }

  Widget _buildStatusTabs() {
    return ListenableBuilder(
      listenable: _pendingCount,
      builder: (context, _) => StatusFilterTabs<InstrumentRentalStatus?>(
        selected: _selectedStatus,
        options: [
          for (final status in InstrumentRentalStatus.values)
            StatusFilterOption(
              label: rentalStatusLabel(status),
              value: status,
              count: status == InstrumentRentalStatus.pending
                  ? _pendingCount.value
                  : null,
            ),
          const StatusFilterOption(label: 'Svi', value: null),
        ],
        onChanged: _onStatusChanged,
      ),
    );
  }

  Widget _buildFilterBar() {
    return AsyncDropdown<InstrumentDto>(
      label: 'Instrument',
      width: _instrumentFilterWidth,
      emptyOptionLabel: 'Svi instrumenti',
      value: _selectedInstrumentId,
      fetcher: () => fetchAllPages(
        (query) => context.read<InstrumentProvider>().search(query),
      ),
      itemLabel: (instrument) => instrument.model,
      itemId: (instrument) => instrument.id,
      onChanged: (value, _) => _onInstrumentChanged(value as int?),
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
            maxWidth: 200,
            cellBuilder: (context, item) => Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                ImageThumbnail(
                  imageUrl: item.instrumentImagePath,
                  apiClient: context.read<ApiClient>(),
                ),
                const SizedBox(width: AppSpacing.sm),
                Flexible(
                  child: Tooltip(
                    message: item.instrumentModel,
                    child: Text(
                      item.instrumentModel,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ),
              ],
            ),
          ),
          ColumnSpec<InstrumentRentalDto>(
            label: 'Student',
            value: (item) => item.studentName ?? 'Nepoznat korisnik',
            maxWidth: 180,
          ),
          ColumnSpec<InstrumentRentalDto>(
            label: 'Status',
            value: _rentalStatusText,
            cellBuilder: (context, item) => StatusChip(
              label: _rentalStatusText(item),
              tone: item.isOverdue
                  ? StatusTone.danger
                  : rentalStatusTone(item.rentalStatus),
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
        statusTabs: _buildStatusTabs(),
        filterBar: _buildFilterBar(),
        fetcher: (page, pageSize, search) =>
            context.read<RentalProvider>().search(
              pagedQuery(
                page,
                pageSize,
                search,
                filters: {
                  if (_selectedStatus != null)
                    'rentalStatus': _selectedStatus!.toJson(),
                  if (_selectedInstrumentId != null)
                    'instrumentId': _selectedInstrumentId,
                },
              ),
            ),
        onRowTap: (context, item) async {
          await RentalDetailDialog.show(context, item.id);
          _listKey.currentState?.refresh();
          _loadPendingCount();
        },
        extraActions: (context, item) =>
            item.rentalStatus == InstrumentRentalStatus.pending
            ? [
                WorkflowActionButtons<RentalTrigger>(
                  actions: rentalTransitionActions(
                    item.rentalStatus,
                    dueAt: item.dueAt,
                  ),
                  compact: true,
                  onRun: (trigger, {note}) =>
                      _runTransition(item, trigger, note: note),
                ),
              ]
            : const <Widget>[],
        rowColor: (item) =>
            item.isOverdue ? StatusTone.danger.background : null,
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
