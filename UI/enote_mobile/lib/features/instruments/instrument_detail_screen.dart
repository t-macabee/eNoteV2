import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../shell/app_router.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/blocked_reason_banner.dart';
import '../../widgets/status_chip.dart';
import '../../widgets/wide_image.dart';
import '../rentals/rental_provider.dart';
import '../rentals/rental_request_sheet.dart';
import 'instrument_provider.dart';

/// The one reason that blocks a rental request, already resolved to the
/// priority order of 02 §4.4.
class _BlockedReason {
  final String copy;
  final String? actionLabel;
  final VoidCallback? onAction;

  const _BlockedReason(this.copy, {this.actionLabel, this.onAction});
}

/// S8 — instrument detail with the request action and its blocking states.
class InstrumentDetailScreen extends StatefulWidget {
  final int instrumentId;

  const InstrumentDetailScreen({super.key, required this.instrumentId});

  @override
  State<InstrumentDetailScreen> createState() => _InstrumentDetailScreenState();
}

class _InstrumentDetailScreenState extends State<InstrumentDetailScreen> {
  InstrumentDto? _instrument;
  RentalDebtDto? _debt;
  Object? _error;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    // Analytics only, and deliberately not awaited: the screen must never
    // wait on it (01 §4.3).
    unawaited(context.read<InstrumentProvider>().recordView(widget.instrumentId));
    _load();
  }

  Future<void> _load() async {
    final instruments = context.read<InstrumentProvider>();
    final rentals = context.read<RentalProvider>();
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final results = await Future.wait<dynamic>([
        instruments.getById(widget.instrumentId),
        rentals.debt(),
      ]);
      if (!mounted) return;
      setState(() {
        _instrument = results[0] as InstrumentDto;
        _debt = results[1] as RentalDebtDto;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e;
        _isLoading = false;
      });
    }
  }

  _BlockedReason? _blockedReason(InstrumentDto instrument) {
    final debt = _debt;
    if (debt != null && debt.hasUnpaidDebt) {
      return _BlockedReason(
        'Imate neizmireno dugovanje od prethodnog iznajmljivanja. '
        'Izmirite ga prije novog zahtjeva.',
        actionLabel: 'Plati sada',
        onAction: debt.rentalId == null
            ? null
            : () => Navigator.of(context)
                  .pushNamed(
                    AppRouter.payment,
                    arguments: PaymentArgs(debt.rentalId!),
                  )
                  .then((_) {
                    // A finished payment clears the debt; reload so the
                    // banner disappears without a manual refresh.
                    if (mounted) _load();
                  }),
      );
    }
    final session = context.read<SessionController>();
    if (!session.isMembershipActive) {
      final until = session.membershipPaidUntil;
      return _BlockedReason(
        until == null
            ? 'Članarina nije aktivna.'
            // formatDate already ends in a period (Bosnian date form), so the
            // copy's sentence stop after {date} is not written again here.
            // S30's MembershipCard renders the same string the same way.
            : 'Članarina je istekla ${formatDate(until)} '
                  'Obratite se školi za obnovu.',
      );
    }
    if (!instrument.isAvailable) {
      return const _BlockedReason('Instrument trenutno nije dostupan.');
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    final instrument = _instrument;
    return Scaffold(
      appBar: AppBar(title: const Text('Instrument')),
      body: instrument == null
          ? AsyncStateView(
              isLoading: _isLoading,
              error: _error,
              onRetry: _load,
              child: const SizedBox.shrink(),
            )
          : _body(instrument),
    );
  }

  Widget _body(InstrumentDto instrument) {
    final blocked = _blockedReason(instrument);
    final description = instrument.description;
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        children: [
          HeroImage(
            imageUrl: instrument.imagePath,
            apiClient: context.read<ApiClient>(),
          ),
          const SizedBox(height: 16),
          Text(
            '${instrument.manufacturer} ${instrument.model}',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 4),
          Text(
            '${instrument.manufacturer} · ${instrument.instrumentType}',
            style: const TextStyle(color: AppTheme.textSecondary),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: Text(
                  instrument.musicStore,
                  style: const TextStyle(color: AppTheme.textSecondary),
                ),
              ),
              instrument.isAvailable
                  ? const StatusChip(
                      label: 'Dostupno',
                      color: AppTheme.success,
                    )
                  : const StatusChip(
                      label: 'Nedostupno',
                      color: AppTheme.textSecondary,
                    ),
            ],
          ),
          if (description != null && description.trim().isNotEmpty) ...[
            const SizedBox(height: 16),
            Text(description),
          ],
          const SizedBox(height: 24),
          if (blocked != null)
            BlockedReasonBanner(
              icon: Icons.warning_amber_outlined,
              reason: blocked.copy,
              actionLabel: blocked.actionLabel,
              onAction: blocked.onAction,
            ),
          const SizedBox(height: 8),
          FilledButton(
            onPressed: blocked != null
                ? null
                : () => showRentalRequestSheet(context, instrument),
            child: const Text('Zatraži iznajmljivanje'),
          ),
        ],
      ),
    );
  }
}
