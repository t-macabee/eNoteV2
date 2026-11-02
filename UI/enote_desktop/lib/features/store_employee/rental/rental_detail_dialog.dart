import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/dialog_shell.dart';
import '../../../widgets/inline_error_text.dart';
import '../../../widgets/status_chip.dart';
import '../../../widgets/workflow_actions.dart';
import 'rental_status_display.dart';
import 'rental_refund_dialog.dart';
import 'rental_provider.dart';

class RentalDetailDialog extends StatefulWidget {
  final int rentalId;

  const RentalDetailDialog({super.key, required this.rentalId});

  static Future<void> show(BuildContext context, int rentalId) {
    return showDialog<void>(
      context: context,
      builder: (_) => RentalDetailDialog(rentalId: rentalId),
    );
  }

  @override
  State<RentalDetailDialog> createState() => _RentalDetailDialogState();
}

class _RentalDetailDialogState extends State<RentalDetailDialog> {
  InstrumentRentalDto? _rental;
  RentalPaymentDto? _payment;
  bool _paymentLoadFailed = false;
  bool _isLoading = true;
  bool _isRefunding = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    setState(() => _isLoading = true);
    try {
      final provider = context.read<RentalProvider>();
      final rental = await provider.getById(widget.rentalId);
      RentalPaymentDto? payment;
      var paymentLoadFailed = false;
      if (rental.isPaid) {
        try {
          payment = await provider.getPaymentStatus(widget.rentalId);
        } catch (_) {
          payment = null;
          paymentLoadFailed = true;
        }
      }
      if (!mounted) return;
      setState(() {
        _rental = rental;
        _payment = payment;
        _paymentLoadFailed = paymentLoadFailed;
      });
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _onRefund() async {
    final provider = context.read<RentalProvider>();
    final payment = _payment;
    final remainingCents = payment == null
        ? null
        : payment.amountCents - (payment.refundedCents ?? 0);
    final amount = await showRefundAmountDialog(
      context,
      maxAmountCents: remainingCents,
    );
    if (amount == null) return;

    final amountCents = amount > 0 ? (amount * 100).round() : null;

    if (!mounted) return;
    setState(() => _isRefunding = true);
    try {
      final result = await provider.refund(
        _rental!.id,
        amountCents: amountCents,
      );
      if (!mounted) return;
      setState(() => _payment = result);
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Povrat obrađen.')));
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) setState(() => _isRefunding = false);
    }
  }

  Future<void> _onTransition(RentalTrigger trigger, {String? note}) async {
    final updated = await context.read<RentalProvider>().transition(
      _rental!.id,
      trigger,
      note: note,
    );
    if (!mounted) return;
    setState(() => _rental = updated);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(rentalTransitionSuccessMessage(trigger))),
    );
  }

  @override
  Widget build(BuildContext context) {
    return DialogShell(
      title: _rental != null
          ? 'Iznajmljivanje - ${_rental!.instrumentModel}'
          : 'Iznajmljivanje',
      width: DialogShellWidth.lg,
      body: _isLoading
          ? const SizedBox(
              height: 200,
              child: Center(child: CircularProgressIndicator()),
            )
          : _rental == null
          ? SizedBox(
              height: 200,
              child: Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Text('Greška pri učitavanju.'),
                    const SizedBox(height: AppSpacing.lg),
                    ElevatedButton(
                      onPressed: _load,
                      child: const Text('Pokušaj ponovo'),
                    ),
                  ],
                ),
              ),
            )
          : _buildBody(),
    );
  }

  Widget _buildBody() {
    final rental = _rental!;
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          StatusChip(
            label: rentalStatusLabel(rental.rentalStatus),
            tone: rentalStatusTone(rental.rentalStatus),
          ),
          const SizedBox(height: AppSpacing.lg),
          _buildInstrumentBlock(rental),
          const SizedBox(height: AppSpacing.lg),
          _buildStudentBlock(rental),
          const SizedBox(height: AppSpacing.lg),
          _buildNotesBlock(rental),
          const SizedBox(height: AppSpacing.lg),
          _buildChargesBlock(rental),
          const SizedBox(height: AppSpacing.xl),
          WorkflowActionButtons<RentalTrigger>(
            actions: rentalTransitionActions(
              rental.rentalStatus,
              dueAt: rental.dueAt,
            ),
            onRun: _onTransition,
          ),
        ],
      ),
    );
  }

  Widget _buildInstrumentBlock(InstrumentRentalDto rental) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Row(
          children: [
            SizedBox(
              width: 100,
              height: 100,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: networkImageOrPlaceholder(
                  rental.instrumentImagePath,
                  context.read<ApiClient>(),
                  size: double.infinity,
                  borderRadius: 0,
                  placeholder: () => Container(
                    color: AppTheme.background,
                    child: const Icon(
                      Icons.piano,
                      size: 40,
                      color: AppTheme.textTertiary,
                    ),
                  ),
                ),
              ),
            ),
            const SizedBox(width: AppSpacing.lg),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    rental.instrumentModel,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    rental.instrumentType,
                    style: const TextStyle(color: AppTheme.textSecondary),
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    rental.storeName,
                    style: const TextStyle(color: AppTheme.textSecondary),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStudentBlock(InstrumentRentalDto rental) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Student', style: Theme.of(context).textTheme.titleSmall),
            const SizedBox(height: AppSpacing.sm),
            Row(
              children: [
                UserAvatar(
                  userId: rental.studentUserId,
                  apiClient: context.read<ApiClient>(),
                  radius: 20,
                  placeholder: () => const CircleAvatar(
                    radius: 20,
                    child: Icon(Icons.person_outline, size: 20),
                  ),
                ),
                const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: Text(
                    rental.studentName ?? 'Nepoznat korisnik',
                    style: const TextStyle(fontWeight: FontWeight.w500),
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            Text(
              'Vremenska linija',
              style: Theme.of(context).textTheme.titleSmall,
            ),
            const SizedBox(height: AppSpacing.sm),
            _timelineRow('Zatraženo', rental.requestedAt),
            if (rental.approvedAt != null)
              _timelineRow('Odobreno', rental.approvedAt!),
            if (rental.rejectedAt != null)
              _timelineRow('Odbijeno', rental.rejectedAt!),
            if (rental.pickedUpAt != null)
              _timelineRow('Preuzeto', rental.pickedUpAt!),
            if (rental.returnedAt != null)
              _timelineRow('Vraćeno', rental.returnedAt!),
          ],
        ),
      ),
    );
  }

  Widget _timelineRow(String label, DateTime date) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.xs),
      child: Row(
        children: [
          SizedBox(
            width: 100,
            child: Text(
              label,
              style: Theme.of(context).textTheme.bodySmall
                  ?.copyWith(color: AppTheme.textSecondary),
            ),
          ),
          Text(
            formatDateTime(date),
            style: Theme.of(context).textTheme.bodySmall,
          ),
        ],
      ),
    );
  }

  Widget _buildNotesBlock(InstrumentRentalDto rental) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (rental.requestNote != null &&
                rental.requestNote!.isNotEmpty) ...[
              Text(
                'Zahtjev (student)',
                style: Theme.of(context).textTheme.titleSmall,
              ),
              const SizedBox(height: AppSpacing.xs),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(AppSpacing.md),
                decoration: BoxDecoration(
                  border: Border.all(color: AppTheme.outline),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(rental.requestNote!),
              ),
              const SizedBox(height: AppSpacing.md),
            ],
            if (rental.note != null && rental.note!.isNotEmpty) ...[
              Text('Odgovor', style: Theme.of(context).textTheme.titleSmall),
              const SizedBox(height: AppSpacing.xs),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(AppSpacing.md),
                decoration: BoxDecoration(
                  border: Border.all(color: AppTheme.outline),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(rental.note!),
              ),
            ],
            if ((rental.requestNote == null || rental.requestNote!.isEmpty) &&
                (rental.note == null || rental.note!.isEmpty))
              const Text(
                'Nema napomena.',
                style: TextStyle(color: AppTheme.textSecondary),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildChargesBlock(InstrumentRentalDto rental) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Naknade', style: Theme.of(context).textTheme.titleSmall),
            const SizedBox(height: AppSpacing.sm),
            _chargeRow('Mjesečna naknada', formatKM(rental.fee)),
            if (rental.dueAt != null)
              _chargeRow('Rok povrata', formatDate(rental.dueAt!)),
            if (rental.hasPenaltyTerms)
              _chargeRow('Penal po danu', formatKM(rental.dailyPenaltyFee)),
            if (rental.hasOverdueDays) ...[
              _chargeRow('Kašnjenje', '${rental.overdueDays} dana'),
              _chargeRow('Penal', formatKM(rental.penaltyFee)),
            ],
            if (rental.totalFee != null) ...[
              _chargeRow('Ukupno', formatKM(rental.totalFee)),
              if (rental.daysCharged != null)
                _chargeRow('Dani', '${rental.daysCharged}'),
            ] else
              _chargeRow('Ukupno', '-'),
            const Divider(height: 24),
            _chargeRow(
              'Plaćeno',
              rental.isPaid
                  ? 'Da (${rental.amountPaid != null ? formatKM(rental.amountPaid) : '-'}${rental.paidAt != null ? ' - ${formatDate(rental.paidAt!)}' : ''})'
                  : 'Ne',
            ),
            if (_paymentLoadFailed) ...[
              const SizedBox(height: AppSpacing.sm),
              const InlineErrorText(
                'Podaci o plaćanju trenutno nisu dostupni.',
              ),
            ],
            if (_payment != null && (_payment!.refundedCents ?? 0) > 0) ...[
              const SizedBox(height: AppSpacing.sm),
              _chargeRow(
                'Povrađeno',
                formatKM(
                  ((_payment!.refundedCents ?? 0) / 100),
                  currency: _payment!.currency,
                ),
              ),
              _chargeRow(
                'Datum povraćaja',
                formatDateNullable(_payment!.refundedAt),
              ),
              _chargeRow(
                'Status',
                _payment!.status == PaymentStatus.refunded
                    ? 'Puni povrat'
                    : 'Djelomični povrat',
              ),
            ],
            if (rental.isPaid &&
                (_payment == null
                        ? 0
                        : _payment!.amountCents -
                              (_payment!.refundedCents ?? 0)) >
                    0) ...[
              const SizedBox(height: AppSpacing.lg),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: _isRefunding ? null : _onRefund,
                  icon: _isRefunding
                      ? const SizedBox(
                          width: AppSize.spinnerSm,
                          height: AppSize.spinnerSm,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.payment_outlined),
                  label: const Text('Refundiraj'),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _chargeRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.xs),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: AppTheme.textSecondary)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }
}
