import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../widgets/paged_list_view.dart';
import '../rentals/rental_provider.dart';
import '../student_notices.dart';
import 'instrument_provider.dart';
import 'instrument_tile.dart';
import 'recommendation_strip.dart';

class InstrumentCatalogScreen extends StatefulWidget {
  const InstrumentCatalogScreen({super.key});

  @override
  State<InstrumentCatalogScreen> createState() =>
      _InstrumentCatalogScreenState();
}

class _InstrumentCatalogScreenState extends State<InstrumentCatalogScreen> {
  static const _bannerPadding = EdgeInsets.fromLTRB(
    AppSpacing.lg,
    AppSpacing.sm,
    AppSpacing.lg,
    0,
  );

  late final PagedFetchController<InstrumentDto> _controller;
  List<InstrumentRecommendationDto> _recommendations = const [];
  RentalDebtDto? _debt;
  bool _debtLoadFailed = false;
  bool _onlyAvailable = true;

  @override
  void initState() {
    super.initState();
    final instruments = context.read<InstrumentProvider>();
    _controller = PagedFetchController<InstrumentDto>(
      pageSize: InstrumentProvider.pageSize,
      fetcher: (page, pageSize, search) => instruments.fetchPage(
        page,
        pageSize,
        search,
        onlyAvailable: _onlyAvailable,
      ),
    );
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadRecommendations();
      _loadDebt();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _loadRecommendations() async {
    try {
      final result = await context.read<InstrumentProvider>().recommended();
      if (mounted) setState(() => _recommendations = result);
    } catch (_) {
      if (mounted) setState(() => _recommendations = const []);
    }
  }

  Future<void> _loadDebt() async {
    try {
      final debt = await context.read<RentalProvider>().debt();
      if (mounted) {
        setState(() {
          _debt = debt;
          _debtLoadFailed = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _debt = null;
          _debtLoadFailed = true;
        });
      }
    }
  }

  void _openInstrument(int instrumentId) {
    Navigator.of(context).pushNamed(
      AppRoutes.instrumentDetail,
      arguments: InstrumentDetailArgs(instrumentId),
    );
  }

  void _openPayment(int rentalId) {
    Navigator.of(context)
        .pushNamed(AppRoutes.payment, arguments: PaymentArgs(rentalId))
        .then((_) {
          if (mounted) _loadDebt();
        });
  }

  void _selectAvailability(bool onlyAvailable) {
    setState(() => _onlyAvailable = onlyAvailable);
    _controller.refresh(resetPage: true);
  }

  @override
  Widget build(BuildContext context) {
    final debt = _debt;
    return Column(
      children: [
        if (debt != null && debt.hasUnpaidDebt)
          Padding(
            padding: _bannerPadding,
            child: StatusBanner(
              icon: Icons.warning_amber_outlined,
              message: rentalDebtNotice,
              tone: StatusTone.danger,
              actionLabel: 'Plati sada',
              onAction: debt.rentalId == null
                  ? null
                  : () => _openPayment(debt.rentalId!),
            ),
          ),
        if (_debtLoadFailed)
          Padding(
            padding: _bannerPadding,
            child: StatusBanner(
              icon: Icons.error_outline,
              message: 'Stanje dugovanja trenutno nije moguće provjeriti.',
              tone: StatusTone.warning,
              actionLabel: 'Pokušaj ponovo',
              onAction: _loadDebt,
            ),
          ),
        RecommendationStrip(
          recommendations: _recommendations,
          onOpen: (instrument) => _openInstrument(instrument.id),
        ),
        Expanded(
          child: PagedListView<InstrumentDto>(
            controller: _controller,
            statusTabs: StatusFilterTabs<bool>(
              scrollable: true,
              selected: _onlyAvailable,
              onChanged: _selectAvailability,
              options: const [
                StatusFilterOption<bool>(label: 'Dostupni', value: true),
                StatusFilterOption<bool>(label: 'Svi', value: false),
              ],
            ),
            itemBuilder: (context, instrument) => InstrumentTile(
              instrument: instrument,
              onTap: () => _openInstrument(instrument.id),
            ),
          ),
        ),
      ],
    );
  }
}
