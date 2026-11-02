import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../widgets/paged_list_view.dart';
import 'rental_provider.dart';
import 'rental_tile.dart';

class RentalListScreen extends StatefulWidget {
  const RentalListScreen({super.key});

  @override
  State<RentalListScreen> createState() => _RentalListScreenState();
}

class _RentalListScreenState extends State<RentalListScreen> {
  late final PagedFetchController<InstrumentRentalDto> _controller;
  RentalProvider? _rentals;
  InstrumentRentalStatus? _status;

  @override
  void initState() {
    super.initState();
    final rentals = context.read<RentalProvider>();
    _controller = PagedFetchController<InstrumentRentalDto>(
      pageSize: RentalProvider.pageSize,
      fetcher: (page, pageSize, _) =>
          rentals.fetchPage(page, pageSize, rentalStatus: _status),
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

  void _onRentalsChanged() {
    if (!mounted) return;
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
      statusTabs: StatusFilterTabs<InstrumentRentalStatus?>(
        scrollable: true,
        selected: _status,
        onChanged: _selectStatus,
        options: [
          for (final status in InstrumentRentalStatus.values)
            StatusFilterOption<InstrumentRentalStatus?>(
              label: rentalStatusLabel(status),
              value: status,
            ),
          const StatusFilterOption<InstrumentRentalStatus?>(
            label: 'Sva',
            value: null,
          ),
        ],
      ),
      itemBuilder: (context, rental) => RentalTile(
        rental: rental,
        onTap: () => Navigator.of(context).pushNamed(
          AppRoutes.rentalDetail,
          arguments: RentalDetailArgs(rental.id),
        ),
      ),
    );
  }
}
