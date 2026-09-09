import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../theme/app_theme.dart';
import '../../widgets/paged_list_view.dart';
import '../../widgets/status_chip.dart';
import 'rental_provider.dart';

/// S10 — the Iznajmljivanja tab root.
///
/// No text search: the server offers none for this endpoint (02 §3.1). The
/// only filter is the status dropdown, whose domain is the seven
/// [InstrumentRentalStatus] values.
class RentalListScreen extends StatefulWidget {
  const RentalListScreen({super.key});

  @override
  State<RentalListScreen> createState() => _RentalListScreenState();
}

class _RentalListScreenState extends State<RentalListScreen> {
  late final PagedFetchController<InstrumentRentalDto> _controller;
  RentalProvider? _rentals;
  InstrumentRentalStatus? _status;
  Object? _error;

  @override
  void initState() {
    super.initState();
    final rentals = context.read<RentalProvider>();
    _controller = PagedFetchController<InstrumentRentalDto>(
      pageSize: RentalProvider.pageSize,
      fetcher: (page, pageSize, _) =>
          rentals.fetchPage(page, pageSize, rentalStatus: _status),
      onError: (error) {
        if (mounted) setState(() => _error = error);
      },
    );
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final rentals = context.read<RentalProvider>();
    if (identical(rentals, _rentals)) return;
    _rentals?.removeListener(_onRentalsChanged);
    _rentals = rentals..addListener(_onRentalsChanged);
  }

  @override
  void dispose() {
    _rentals?.removeListener(_onRentalsChanged);
    _controller.dispose();
    super.dispose();
  }

  /// A create or a cancel elsewhere in the app (the request sheet, the detail
  /// screen) notifies the provider; the list reloads from page 1 so the new
  /// rental shows on top (server order is `RequestedAt` desc).
  void _onRentalsChanged() {
    if (!mounted) return;
    setState(() => _error = null);
    _controller.refresh(resetPage: true);
  }

  void _selectStatus(InstrumentRentalStatus? status) {
    setState(() => _status = status);
    _controller.refresh(resetPage: true);
  }

  @override
  Widget build(BuildContext context) {
    return PagedListView<InstrumentRentalDto>(
      controller: _controller,
      showSearch: false,
      emptyMessage: 'Još nemate iznajmljivanja.',
      error: _error,
      onRetry: () {
        setState(() => _error = null);
        _controller.load();
      },
      filterRow: Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
        child: DropdownButtonFormField<InstrumentRentalStatus?>(
          initialValue: _status,
          decoration: const InputDecoration(labelText: 'Status'),
          items: [
            const DropdownMenuItem<InstrumentRentalStatus?>(
              value: null,
              child: Text('—'),
            ),
            for (final status in InstrumentRentalStatus.values)
              DropdownMenuItem<InstrumentRentalStatus?>(
                value: status,
                child: Text(StatusChip.rental(status).label),
              ),
          ],
          onChanged: _selectStatus,
        ),
      ),
      itemBuilder: (context, rental) => _RentalRow(
        rental: rental,
        onTap: () => Navigator.of(context).pushNamed(
          AppRouter.rentalDetail,
          arguments: RentalDetailArgs(rental.id),
        ),
      ),
    );
  }
}

class _RentalRow extends StatelessWidget {
  final InstrumentRentalDto rental;
  final VoidCallback onTap;

  const _RentalRow({required this.rental, required this.onTap});

  /// Payment is only meaningful once the instrument came back (02 §3.1).
  bool get _showsPaidMark =>
      rental.rentalStatus == InstrumentRentalStatus.completed ||
      rental.rentalStatus == InstrumentRentalStatus.returnedEarly;

  @override
  Widget build(BuildContext context) {
    // Laid out by hand rather than with a ListTile: the status chip stacked
    // over the payment mark is taller than a tile's trailing slot allows.
    return Column(
      children: [
        InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                ImageThumbnail(
                  imageUrl: rental.instrumentImagePath,
                  apiClient: context.read<ApiClient>(),
                  size: 56,
                  borderRadius: 8,
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        rental.instrumentModel,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: 2),
                      Text(
                        '${rental.storeName} · ${formatDate(rental.requestedAt)}',
                        style: const TextStyle(
                          color: AppTheme.textSecondary,
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 8),
                Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    StatusChip.rental(rental.rentalStatus),
                    if (_showsPaidMark)
                      Text(
                        rental.isPaid ? 'Plaćeno' : 'Nije plaćeno',
                        style: TextStyle(
                          fontSize: 11,
                          color: rental.isPaid
                              ? AppTheme.success
                              : AppTheme.textSecondary,
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ),
        const Divider(height: 1),
      ],
    );
  }
}
