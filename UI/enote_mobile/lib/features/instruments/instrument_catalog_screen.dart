import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../theme/app_theme.dart';
import '../../widgets/blocked_reason_banner.dart';
import '../../widgets/paged_list_view.dart';
import '../../widgets/status_chip.dart';
import '../rentals/rental_provider.dart';
import 'instrument_provider.dart';
import 'recommendation_strip.dart';

/// S7 — the Instrumenti tab root: debt banner, recommendation strip and the
/// paged catalogue.
///
/// The three first-load calls (`recommended`, catalogue page 1, `debt`) are
/// independent and all start on the first frame: this widget's [initState]
/// fires the recommendation and debt calls while the [PagedListView] it
/// builds fires the page load (01 §6.5).
class InstrumentCatalogScreen extends StatefulWidget {
  const InstrumentCatalogScreen({super.key});

  @override
  State<InstrumentCatalogScreen> createState() =>
      _InstrumentCatalogScreenState();
}

class _InstrumentCatalogScreenState extends State<InstrumentCatalogScreen> {
  late final PagedFetchController<InstrumentDto> _controller;
  List<InstrumentRecommendationDto> _recommendations = const [];
  RentalDebtDto? _debt;
  Object? _error;
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
      onError: (error) {
        if (mounted) setState(() => _error = error);
      },
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
      // The strip is an extra: a failure hides it, it never errors (02 §5).
      if (mounted) setState(() => _recommendations = const []);
    }
  }

  Future<void> _loadDebt() async {
    try {
      final debt = await context.read<RentalProvider>().debt();
      if (mounted) setState(() => _debt = debt);
    } catch (_) {
      if (mounted) setState(() => _debt = null);
    }
  }

  void _openInstrument(int instrumentId) {
    Navigator.of(context).pushNamed(
      AppRouter.instrumentDetail,
      arguments: InstrumentDetailArgs(instrumentId),
    );
  }

  void _openPayment(int rentalId) {
    Navigator.of(
      context,
    ).pushNamed(AppRouter.payment, arguments: PaymentArgs(rentalId));
  }

  void _toggleAvailable(bool value) {
    setState(() => _onlyAvailable = value);
    _controller.refresh(resetPage: true);
  }

  @override
  Widget build(BuildContext context) {
    final debt = _debt;
    return Column(
      children: [
        if (debt != null && debt.hasUnpaidDebt)
          BlockedReasonBanner(
            icon: Icons.warning_amber_outlined,
            reason:
                'Imate neizmireno dugovanje od prethodnog iznajmljivanja. '
                'Izmirite ga prije novog zahtjeva.',
            actionLabel: 'Plati sada',
            onAction: debt.rentalId == null
                ? null
                : () => _openPayment(debt.rentalId!),
          ),
        RecommendationStrip(
          recommendations: _recommendations,
          onOpen: (instrument) => _openInstrument(instrument.id),
        ),
        Expanded(
          child: PagedListView<InstrumentDto>(
            controller: _controller,
            error: _error,
            onRetry: () {
              setState(() => _error = null);
              _controller.load();
            },
            filterRow: Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 0),
              child: Row(
                children: [
                  Switch(value: _onlyAvailable, onChanged: _toggleAvailable),
                  const SizedBox(width: 8),
                  const Text('Samo dostupni'),
                ],
              ),
            ),
            itemBuilder: (context, instrument) => _InstrumentRow(
              instrument: instrument,
              onTap: () => _openInstrument(instrument.id),
            ),
          ),
        ),
      ],
    );
  }
}

class _InstrumentRow extends StatelessWidget {
  final InstrumentDto instrument;
  final VoidCallback onTap;

  const _InstrumentRow({required this.instrument, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        ListTile(
          onTap: onTap,
          leading: ImageThumbnail(
            imageUrl: instrument.imagePath,
            apiClient: context.read<ApiClient>(),
            size: 56,
            borderRadius: 8,
          ),
          title: Text('${instrument.manufacturer} ${instrument.model}'),
          subtitle: Text(
            '${instrument.instrumentType} · ${instrument.musicStore}',
            style: const TextStyle(color: AppTheme.textSecondary),
          ),
          trailing: instrument.isAvailable
              ? const StatusChip(label: 'Dostupno', color: AppTheme.success)
              : const StatusChip(
                  label: 'Nedostupno',
                  color: AppTheme.textSecondary,
                ),
        ),
        const Divider(height: 1),
      ],
    );
  }
}
