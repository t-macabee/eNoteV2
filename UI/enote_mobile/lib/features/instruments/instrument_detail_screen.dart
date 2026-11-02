import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../shell/routes.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/detail_actions.dart';
import '../../widgets/detail_header.dart';
import '../../widgets/detail_section.dart';
import '../../widgets/wide_image.dart';
import '../rentals/rental_provider.dart';
import '../rentals/rental_request_sheet.dart';
import '../student_notices.dart';
import 'instrument_labels.dart';
import 'instrument_provider.dart';

class _BlockedReason {
  final String copy;
  final StatusTone tone;
  final String? actionLabel;
  final VoidCallback? onAction;

  const _BlockedReason(this.copy, this.tone, {this.actionLabel, this.onAction});
}

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
    unawaited(
      context.read<InstrumentProvider>().recordView(widget.instrumentId),
    );
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
        rentalDebtNotice,
        StatusTone.danger,
        actionLabel: 'Plati sada',
        onAction: debt.rentalId == null
            ? null
            : () => Navigator.of(context)
                  .pushNamed(
                    AppRoutes.payment,
                    arguments: PaymentArgs(debt.rentalId!),
                  )
                  .then((_) {
                    if (mounted) _load();
                  }),
      );
    }
    final session = context.read<SessionController>();
    if (!session.isMembershipActive) {
      return _BlockedReason(
        membershipNotice(session.membershipPaidUntil),
        StatusTone.danger,
      );
    }
    if (!instrument.isAvailable) {
      return const _BlockedReason(
        'Instrument trenutno nije dostupan.',
        StatusTone.neutral,
      );
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
        padding: const EdgeInsets.only(bottom: AppSpacing.lg),
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.lg,
              AppSpacing.lg,
              AppSpacing.lg,
              0,
            ),
            child: HeroImage(
              imageUrl: instrument.imagePath,
              apiClient: context.read<ApiClient>(),
            ),
          ),
          DetailHeader(
            title: '${instrument.manufacturer} ${instrument.model}',
            subtitle: '${instrument.instrumentType} · ${instrument.musicStore}',
            chips: [instrumentAvailabilityChip(instrument.isAvailable)],
          ),
          if (description != null && description.trim().isNotEmpty)
            DetailSection(title: 'Opis', child: Text(description)),
          DetailActions(
            banner: blocked == null
                ? null
                : StatusBanner(
                    icon: Icons.warning_amber_outlined,
                    message: blocked.copy,
                    tone: blocked.tone,
                    actionLabel: blocked.actionLabel,
                    onAction: blocked.onAction,
                  ),
            actions: [
              FilledButton(
                onPressed: blocked != null
                    ? null
                    : () => showRentalRequestSheet(context, instrument),
                child: const Text('Zatraži iznajmljivanje'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
