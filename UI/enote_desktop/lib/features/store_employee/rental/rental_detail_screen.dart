import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/rental_transition_action_row.dart';
import 'rental_provider.dart';

class RentalDetailScreen extends StatefulWidget {
  final int rentalId;

  const RentalDetailScreen({super.key, required this.rentalId});

  @override
  State<RentalDetailScreen> createState() => _RentalDetailScreenState();
}

class _RentalDetailScreenState extends State<RentalDetailScreen> {
  InstrumentRentalDto? _rental;
  RentalPaymentDto? _payment;
  bool _isLoading = true;
  bool _isTransitioning = false;
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
      if (rental.isPaid) {
        try {
          payment = await provider.getPaymentStatus(widget.rentalId);
        } catch (_) {
          // Best-effort: a payment-status hiccup shouldn't blank the
          // whole screen when the rental itself loaded successfully.
          payment = null;
        }
      }
      if (!mounted) return;
      setState(() {
        _rental = rental;
        _payment = payment;
      });
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: e.toString());
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _onRefund() async {
    final provider = context.read<RentalProvider>();
    final input = await _promptForRefundAmount();
    if (input == null) return;

    int? amountCents;
    if (input.isNotEmpty) {
      final amount = double.parse(input.replaceAll(',', '.'));
      amountCents = (amount * 100).round();
    }

    setState(() => _isRefunding = true);
    try {
      final result = await provider.refund(
        _rental!.id,
        amountCents: amountCents,
      );
      if (!mounted) return;
      setState(() => _payment = result);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Povrat obrađen.')),
      );
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: e.toString());
      }
    } finally {
      if (mounted) setState(() => _isRefunding = false);
    }
  }

  Future<String?> _promptForRefundAmount() {
    final controller = TextEditingController();
    return showDialog<String>(
      context: context,
      barrierDismissible: false,
      builder: (context) => StatefulBuilder(builder: (ctx, setLocal) {
        final text = controller.text.trim();
        final hasValue = text.isNotEmpty;
        final parsed = hasValue ? double.tryParse(text.replaceAll(',', '.')) : null;
        final invalid = hasValue && (parsed == null || parsed < 0);
        return AlertDialog(
          title: const Text('Refundiraj'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text('Unesite iznos za djelomični povrat. '
                  'Ostavite prazno za puni povrat.'),
              const SizedBox(height: 12),
              TextField(
                controller: controller,
                autofocus: true,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                decoration: InputDecoration(
                  labelText: 'Iznos (KM)',
                  hintText: 'Prazno = puni povrat',
                  errorText: invalid ? 'Unesite važeći iznos.' : null,
                  border: const OutlineInputBorder(),
                ),
                onChanged: (_) => setLocal(() {}),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () {
                controller.dispose();
                Navigator.pop(context);
              },
              child: const Text('Otkaži'),
            ),
            ElevatedButton(
              onPressed: invalid
                  ? null
                  : () {
                      final input = controller.text.trim();
                      controller.dispose();
                      Navigator.pop(context, input);
                    },
              child: const Text('Potvrdi'),
            ),
          ],
        );
      }),
    );
  }

  Future<void> _onTransition(RentalTrigger trigger, {String? note}) async {
    setState(() => _isTransitioning = true);
    try {
      final p = context.read<RentalProvider>();
      final updated = switch (trigger) {
        RentalTrigger.approve => await p.approve(_rental!.id, note: note),
        RentalTrigger.reject => await p.reject(_rental!.id, note: note),
        RentalTrigger.pickup => await p.pickup(_rental!.id, note: note),
        RentalTrigger.complete => await p.complete(_rental!.id, note: note),
        RentalTrigger.returnEarly => await p.returnEarly(_rental!.id, note: note),
        _ => throw StateError('Student-only trigger on StoreEmployee screen'),
      };
      if (!mounted) return;
      setState(() => _rental = updated);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Status ažuriran.')),
      );
    } finally {
      if (mounted) setState(() => _isTransitioning = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: true,
      onPopInvokedWithResult: (didPop, _) {
        if (didPop && _rental != null) {
          Navigator.of(context).pop(true);
        }
      },
      child: Scaffold(
        appBar: AppBar(
          title: Text(_rental != null
              ? 'Iznajmljivanje #${_rental!.id} — ${_rental!.instrumentModel}'
              : 'Iznajmljivanje'),
        ),
        body: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _rental == null
                ? Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Text('Greška pri učitavanju.'),
                        const SizedBox(height: 16),
                        ElevatedButton(
                          onPressed: _load,
                          child: const Text('Pokušaj ponovo'),
                        ),
                      ],
                    ),
                  )
                : _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    final rental = _rental!;
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildStatusChip(rental.rentalStatus),
          const SizedBox(height: 16),
          _buildInstrumentBlock(rental),
          const SizedBox(height: 16),
          _buildStudentBlock(rental),
          const SizedBox(height: 16),
          _buildNotesBlock(rental),
          const SizedBox(height: 16),
          _buildChargesBlock(rental),
          const SizedBox(height: 24),
          RentalTransitionActionRow(
            status: rental.rentalStatus,
            enabled: !_isTransitioning,
            onTransition: _onTransition,
          ),
        ],
      ),
    );
  }

  Widget _buildStatusChip(InstrumentRentalStatus status) {
    final label = _statusLabel(status);
    final color = _statusColor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  Widget _buildInstrumentBlock(InstrumentRentalDto rental) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            if (rental.instrumentImagePath != null)
              ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: ImageField(
                  imageUrl: rental.instrumentImagePath,
                  baseUrl: context.read<ApiClient>().baseUrl,
                  tokenProvider: () => context.read<AuthState>().accessToken,
                  editable: false,
                  size: 100,
                ),
              )
            else
              Container(
                width: 100,
                height: 100,
                decoration: BoxDecoration(
                  color: Colors.grey.shade200,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Icon(Icons.piano, size: 40, color: Colors.grey),
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
                  const SizedBox(height: 4),
                  Text(rental.instrumentType, style: const TextStyle(color: Colors.grey)),
                  const SizedBox(height: 4),
                  Text(rental.storeName, style: const TextStyle(color: Colors.grey)),
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
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Student', style: Theme.of(context).textTheme.titleSmall),
            const SizedBox(height: 8),
            Text(
              rental.studentName ?? 'Student #${rental.studentUserId}',
              style: const TextStyle(fontWeight: FontWeight.w500),
            ),
            const Divider(height: 24),
            Text('Vremenska linija', style: Theme.of(context).textTheme.titleSmall),
            const SizedBox(height: 8),
            _timelineRow('Zatraženo', rental.requestedAt),
            if (rental.approvedAt != null)
              _timelineRow('Odobreno', rental.approvedAt!, 'by #${rental.approvedById}'),
            if (rental.rejectedAt != null)
              _timelineRow('Odbijeno', rental.rejectedAt!, 'by #${rental.rejectedById}'),
            if (rental.pickedUpAt != null)
              _timelineRow('Preuzeto', rental.pickedUpAt!),
            if (rental.returnedAt != null)
              _timelineRow('Vraćeno', rental.returnedAt!),
          ],
        ),
      ),
    );
  }

  Widget _timelineRow(String label, DateTime date, [String? suffix]) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Row(
        children: [
          SizedBox(
            width: 100,
            child: Text(label, style: const TextStyle(color: Colors.grey, fontSize: 13)),
          ),
          Text(
            '${date.day}.${date.month}.${date.year}. ${date.hour}:${date.minute.toString().padLeft(2, '0')}',
            style: const TextStyle(fontSize: 13),
          ),
          if (suffix != null)
            Padding(
              padding: const EdgeInsets.only(left: 8),
              child: Text(suffix, style: const TextStyle(color: Colors.grey, fontSize: 12)),
            ),
        ],
      ),
    );
  }

  Widget _buildNotesBlock(InstrumentRentalDto rental) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (rental.requestNote != null && rental.requestNote!.isNotEmpty) ...[
              Text('Zahtjev (student)', style: Theme.of(context).textTheme.titleSmall),
              const SizedBox(height: 4),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  border: Border.all(color: Colors.grey.shade300),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(rental.requestNote!),
              ),
              const SizedBox(height: 12),
            ],
            if (rental.note != null && rental.note!.isNotEmpty) ...[
              Text('Odgovor', style: Theme.of(context).textTheme.titleSmall),
              const SizedBox(height: 4),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  border: Border.all(color: Colors.grey.shade300),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(rental.note!),
              ),
            ],
            if ((rental.requestNote == null || rental.requestNote!.isEmpty) &&
                (rental.note == null || rental.note!.isEmpty))
              const Text('Nema napomena.', style: TextStyle(color: Colors.grey)),
          ],
        ),
      ),
    );
  }

  Widget _buildChargesBlock(InstrumentRentalDto rental) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Naknade', style: Theme.of(context).textTheme.titleSmall),
            const SizedBox(height: 8),
            _chargeRow('Mjesečna naknada', '${rental.fee.toStringAsFixed(2)} KM'),
            if (rental.totalFee != null) ...[
              _chargeRow(
                rental.isProrated ? 'Ukupno (proporcionalno)' : 'Ukupno',
                '${rental.totalFee!.toStringAsFixed(2)} KM',
              ),
              if (rental.monthsCharged != null)
                _chargeRow('Mjeseci', '${rental.monthsCharged}'),
              if (rental.daysCharged != null)
                _chargeRow('Dani', '${rental.daysCharged}'),
            ] else
              _chargeRow('Ukupno', '-'),
            const Divider(height: 24),
            _chargeRow(
              'Plaćeno',
              rental.isPaid
                  ? 'Da (${rental.amountPaid?.toStringAsFixed(2) ?? '-'} KM${rental.paidAt != null ? ' — ${rental.paidAt!.day}.${rental.paidAt!.month}.${rental.paidAt!.year}.' : ''})'
                  : 'Ne',
            ),
            if (_payment != null && (_payment!.refundedCents ?? 0) > 0) ...[
              const SizedBox(height: 8),
              _chargeRow(
                'Povrađeno',
                '${((_payment!.refundedCents ?? 0) / 100).toStringAsFixed(2)} KM',
              ),
              _chargeRow(
                'Datum povraćaja',
                '${_payment!.refundedAt!.day}.${_payment!.refundedAt!.month}.${_payment!.refundedAt!.year}.',
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
                    : _payment!.amountCents - (_payment!.refundedCents ?? 0)) > 0) ...[
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: _isRefunding ? null : _onRefund,
                  icon: _isRefunding
                      ? const SizedBox(
                          width: 16,
                          height: 16,
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
      padding: const EdgeInsets.only(bottom: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: Colors.grey)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }

  static String _statusLabel(InstrumentRentalStatus status) => switch (status) {
        InstrumentRentalStatus.pending => 'Na čekanju',
        InstrumentRentalStatus.approved => 'Odobreno',
        InstrumentRentalStatus.active => 'Aktivno',
        InstrumentRentalStatus.completed => 'Završeno',
        InstrumentRentalStatus.rejected => 'Odbijeno',
        InstrumentRentalStatus.canceled => 'Otkazano',
        InstrumentRentalStatus.returnedEarly => 'Prijevremeni povrat',
      };

  static Color _statusColor(InstrumentRentalStatus status) => switch (status) {
        InstrumentRentalStatus.pending => Colors.orange,
        InstrumentRentalStatus.approved => Colors.blue,
        InstrumentRentalStatus.active => Colors.green,
        InstrumentRentalStatus.completed => Colors.grey,
        InstrumentRentalStatus.rejected => Colors.red,
        InstrumentRentalStatus.canceled => Colors.red.shade300,
        InstrumentRentalStatus.returnedEarly => Colors.purple,
      };
}
