import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/labeled_value.dart';
import '../../widgets/section_header.dart';
import '../../widgets/status_chip.dart';
import '../payments/payment_provider.dart';
import 'rental_provider.dart';
import 'rental_timeline.dart';

/// S11 — rental detail: header, timeline, fees, and the two actions.
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

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final rentals = context.read<RentalProvider>();
    final payments = context.read<PaymentProvider>();
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

  Future<void> _cancel(InstrumentRentalDto rental) async {
    final rentals = context.read<RentalProvider>();
    final messenger = ScaffoldMessenger.of(context);
    final confirmed = await confirmDialog(
      context: context,
      title: 'Otkazivanje zahtjeva',
      message: 'Želite li otkazati zahtjev za ${rental.instrumentModel}?',
    );
    if (confirmed != true) return;

    setState(() => _isCancelling = true);
    try {
      await rentals.cancel(rental.id);
      await _load();
      if (!mounted) return;
      messenger.showSnackBar(
        const SnackBar(content: Text('Zahtjev je otkazan.')),
      );
    } catch (e) {
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(content: Text(userMessage(e))));
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
                padding: const EdgeInsets.only(bottom: 24),
                children: [
                  _header(rental),
                  const Divider(height: 1),
                  const SectionHeader(title: 'Vremenska linija'),
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
                    child: RentalTimeline(rental: rental),
                  ),
                  const Divider(height: 1),
                  const SectionHeader(title: 'Naknade'),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: _fees(rental),
                  ),
                  const SizedBox(height: 24),
                  ..._actions(rental),
                ],
              ),
            ),
    );
  }

  Widget _header(InstrumentRentalDto rental) {
    final payment = _payment;
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ImageThumbnail(
            imageUrl: rental.instrumentImagePath,
            apiClient: context.read<ApiClient>(),
            size: 72,
            borderRadius: 8,
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  rental.instrumentModel,
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: 2),
                Text(
                  rental.instrumentType,
                  style: const TextStyle(color: AppTheme.textSecondary),
                ),
                Text(
                  rental.storeName,
                  style: const TextStyle(color: AppTheme.textSecondary),
                ),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  runSpacing: 4,
                  children: [
                    StatusChip.rental(rental.rentalStatus),
                    if (rental.isPaid)
                      StatusChip.payment(PaymentStatus.succeeded),
                    if (payment != null &&
                        (payment.status == PaymentStatus.refunded ||
                            payment.status == PaymentStatus.partiallyRefunded))
                      StatusChip.payment(payment.status),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _fees(InstrumentRentalDto rental) {
    final payment = _payment;
    final refundedCents = payment?.refundedCents ?? 0;
    return Column(
      children: [
        LabeledValue(
          label: 'Mjesečna naknada',
          value: formatKM(rental.fee),
        ),
        LabeledValue(label: 'Obračun', value: _charge(rental)),
        LabeledValue(label: 'Ukupno', value: formatKM(rental.totalFee)),
        LabeledValue(label: 'Plaćeno', value: _paid(rental)),
        if (payment != null && refundedCents > 0)
          LabeledValue(label: 'Povrat', value: _refund(payment)),
      ],
    );
  }

  String _charge(InstrumentRentalDto rental) {
    final months = rental.monthsCharged;
    final days = rental.daysCharged;
    if (months == null && days == null) return '—';
    final base = '${months ?? 0} mj. + ${days ?? 0} dana';
    return rental.isProrated ? '$base · proporcionalno' : base;
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
      formatKM((payment.refundedCents ?? 0) / 100),
      if (payment.refundedAt != null) formatDate(payment.refundedAt!),
      if (payment.status == PaymentStatus.refunded ||
          payment.status == PaymentStatus.partiallyRefunded)
        StatusChip.payment(payment.status).label,
    ];
    return parts.join(' · ');
  }

  /// `[ Plati ]` is bound to the server's own payable predicate (02 §10.1):
  /// the instrument was actually picked up, it has come back, the total is
  /// known and nothing has been paid yet.
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

  List<Widget> _actions(InstrumentRentalDto rental) {
    return [
      if (_isPayable(rental))
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: FilledButton(
            onPressed: () => Navigator.of(context)
                .pushNamed(
                  AppRouter.payment,
                  arguments: PaymentArgs(rental.id),
                )
                .then((_) {
                  // A finished payment flips isPaid server-side; reload so
                  // S11 shows Plaćeno without a manual refresh.
                  if (mounted) _load();
                }),
            child: Text('Plati ${formatKM(rental.totalFee)}'),
          ),
        ),
      if (_isCancellable(rental)) ...[
        const SizedBox(height: 8),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: OutlinedButton(
            onPressed: _isCancelling ? null : () => _cancel(rental),
            style: OutlinedButton.styleFrom(
              foregroundColor: AppTheme.error,
              side: const BorderSide(color: AppTheme.error),
            ),
            child: const Text('Otkaži zahtjev'),
          ),
        ),
      ],
    ];
  }
}
