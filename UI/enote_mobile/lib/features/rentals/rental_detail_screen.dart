import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/detail_actions.dart';
import '../../widgets/detail_header.dart';
import '../../widgets/detail_section.dart';
import '../../widgets/labeled_value.dart';
import '../../widgets/submit_button.dart';
import 'payments/rental_payment_provider.dart';
import 'rental_provider.dart';
import 'rental_timeline.dart';

class RentalDetailScreen extends StatefulWidget {
  final int rentalId;

  const RentalDetailScreen({super.key, required this.rentalId});

  @override
  State<RentalDetailScreen> createState() => _RentalDetailScreenState();
}

class _RentalDetailScreenState extends State<RentalDetailScreen> {
  InstrumentRentalDto? _rental;
  RentalPaymentDto? _payment;
  Object? _error;
  bool _isLoading = true;
  bool _isCancelling = false;
  bool _isRenewing = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final rentals = context.read<RentalProvider>();
    final payments = context.read<RentalPaymentProvider>();
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final results = await Future.wait<dynamic>([
        rentals.getById(widget.rentalId),
        payments.status(widget.rentalId),
      ]);
      if (!mounted) return;
      setState(() {
        _rental = results[0] as InstrumentRentalDto;
        _payment = results[1] as RentalPaymentDto?;
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

  Future<void> _renew(InstrumentRentalDto rental) async {
    final rentals = context.read<RentalProvider>();
    final messenger = ScaffoldMessenger.of(context);
    final confirmed = await confirmDialog(
      context: context,
      title: 'Produženje iznajmljivanja',
      message:
          'Produžiti iznajmljivanje instrumenta ${rental.instrumentModel} za još jedan period?',
    );
    if (confirmed != true || !mounted) return;

    setState(() => _isRenewing = true);
    try {
      final updated = await rentals.renew(rental.id);
      await _load();
      if (!mounted) return;
      messenger.showSnackBar(
        SnackBar(
          content: Text(
            'Iznajmljivanje je produženo do ${formatDate(updated.dueAt!)}.',
          ),
        ),
      );
    } catch (e) {
      if (!mounted) return;
      ErrorBanner.show(context, message: userMessage(e));
    } finally {
      if (mounted) setState(() => _isRenewing = false);
    }
  }

  Future<void> _cancel(InstrumentRentalDto rental) async {
    final rentals = context.read<RentalProvider>();
    final messenger = ScaffoldMessenger.of(context);
    final reason = await promptForReason(
      context,
      title: 'Otkazivanje zahtjeva',
    );
    if (!mounted || reason == null) return;

    setState(() => _isCancelling = true);
    try {
      await rentals.cancel(rental.id, note: reason);
      await _load();
      if (!mounted) return;
      messenger.showSnackBar(
        const SnackBar(content: Text('Zahtjev je otkazan.')),
      );
    } catch (e) {
      if (!mounted) return;
      ErrorBanner.show(context, message: userMessage(e));
    } finally {
      if (mounted) setState(() => _isCancelling = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final rental = _rental;
    return Scaffold(
      appBar: AppBar(title: const Text('Iznajmljivanje')),
      body: rental == null
          ? AsyncStateView(
              isLoading: _isLoading,
              error: _error,
              onRetry: _load,
              child: const SizedBox.shrink(),
            )
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.only(bottom: AppSpacing.lg),
                children: [
                  _header(rental),
                  DetailSection(
                    title: 'Vremenska linija',
                    child: RentalTimeline(rental: rental),
                  ),
                  DetailSection(title: 'Naknade', child: _fees(rental)),
                  _actions(rental),
                ],
              ),
            ),
    );
  }

  Widget _header(InstrumentRentalDto rental) {
    final payment = _payment;
    return DetailHeader(
      leading: ImageThumbnail(
        imageUrl: rental.instrumentImagePath,
        apiClient: context.read<ApiClient>(),
        size: AppSize.headerThumbnail,
        borderRadius: AppRadius.md,
      ),
      title: rental.instrumentModel,
      subtitle: '${rental.instrumentType} · ${rental.storeName}',
      chips: [
        StatusChip(
          label: rentalStatusLabel(rental.rentalStatus),
          tone: rentalStatusTone(rental.rentalStatus),
        ),
        if (rental.isPaid)
          StatusChip(
            label: paymentStatusLabel(PaymentStatus.succeeded),
            tone: paymentStatusTone(PaymentStatus.succeeded),
          ),
        if (payment != null &&
            (payment.status == PaymentStatus.refunded ||
                payment.status == PaymentStatus.partiallyRefunded))
          StatusChip(
            label: paymentStatusLabel(payment.status),
            tone: paymentStatusTone(payment.status),
          ),
        if (!rental.instrumentIsActive)
          const StatusChip(
            label: 'Instrument više nije u ponudi',
            tone: StatusTone.danger,
          ),
      ],
      onTap: rental.instrumentIsActive ? () => _openInstrument(rental) : null,
    );
  }

  void _openInstrument(InstrumentRentalDto rental) {
    Navigator.of(context).pushNamed(
      AppRoutes.instrumentDetail,
      arguments: InstrumentDetailArgs(rental.instrumentId),
    );
  }

  Widget _fees(InstrumentRentalDto rental) {
    final payment = _payment;
    final refundedCents = payment?.refundedCents ?? 0;
    return Column(
      children: [
        LabeledValue(label: 'Mjesečna naknada', value: formatKM(rental.fee)),
        if (rental.dueAt != null)
          LabeledValue(label: 'Rok povrata', value: formatDate(rental.dueAt!)),
        if (rental.hasPenaltyTerms)
          LabeledValue(
            label: 'Penal po danu kašnjenja',
            value: formatKM(rental.dailyPenaltyFee),
          ),
        if (rental.hasOverdueDays)
          LabeledValue(
            label: 'Kašnjenje',
            value:
                '${rental.overdueDays} dana · ${formatKM(rental.penaltyFee)}',
          ),
        LabeledValue(label: 'Obračun', value: rental.chargeLabel),
        LabeledValue(label: 'Ukupno', value: formatKM(rental.totalFee)),
        LabeledValue(label: 'Plaćeno', value: _paid(rental)),
        if (payment != null && refundedCents > 0)
          LabeledValue(label: 'Povrat', value: _refund(payment)),
      ],
    );
  }

  String _paid(InstrumentRentalDto rental) {
    if (!rental.isPaid) return 'Ne';
    final parts = <String>[
      'Da',
      if (rental.amountPaid != null) formatKM(rental.amountPaid),
      if (rental.paidAt != null) formatDate(rental.paidAt!),
    ];
    return parts.join(' · ');
  }

  String _refund(RentalPaymentDto payment) {
    final parts = <String>[
      formatKM((payment.refundedCents ?? 0) / 100, currency: payment.currency),
      if (payment.refundedAt != null) formatDate(payment.refundedAt!),
      if (payment.status == PaymentStatus.refunded ||
          payment.status == PaymentStatus.partiallyRefunded)
        paymentStatusLabel(payment.status),
    ];
    return parts.join(' · ');
  }

  bool _isPayable(InstrumentRentalDto rental) {
    final isReturned =
        rental.rentalStatus == InstrumentRentalStatus.completed ||
        rental.rentalStatus == InstrumentRentalStatus.returnedEarly;
    return isReturned &&
        rental.pickedUpAt != null &&
        !rental.isPaid &&
        rental.totalFee != null;
  }

  bool _isCancellable(InstrumentRentalDto rental) =>
      rental.rentalStatus == InstrumentRentalStatus.pending ||
      rental.rentalStatus == InstrumentRentalStatus.approved;

  Widget _actions(InstrumentRentalDto rental) {
    final now = DateTime.now();
    final isRenewable = rental.isRenewableAt(now);
    final isActive = rental.rentalStatus == InstrumentRentalStatus.active;
    return DetailActions(
      banner: isActive && !isRenewable
          ? StatusBanner(
              icon: Icons.info_outline,
              message:
                  rental.renewalOpensAt != null &&
                      now.isBefore(rental.renewalOpensAt!)
                  ? 'Produženje je moguće od ${formatDate(rental.renewalOpensAt!)}.'
                  : 'Rok je istekao. Vratite instrument u prodavnicu.',
              tone: StatusTone.warning,
            )
          : null,
      actions: [
        if (isActive)
          SubmitButton(
            label: 'Produži iznajmljivanje',
            onPressed: isRenewable ? () => _renew(rental) : null,
            isBusy: _isRenewing,
          ),
        if (_isPayable(rental))
          FilledButton(
            onPressed: () => Navigator.of(context)
                .pushNamed(AppRoutes.payment, arguments: PaymentArgs(rental.id))
                .then((_) {
                  if (mounted) _load();
                }),
            child: Text('Plati ${formatKM(rental.totalFee)}'),
          ),
        if (_isCancellable(rental))
          OutlinedButton(
            onPressed: _isCancelling ? null : () => _cancel(rental),
            style: dangerOutlinedButtonStyle,
            child: const Text('Otkaži zahtjev'),
          ),
      ],
    );
  }
}
