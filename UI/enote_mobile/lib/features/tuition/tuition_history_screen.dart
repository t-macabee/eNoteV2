import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/paged_list_view.dart';
import '../../widgets/status_chip.dart';
import 'tuition_payment_provider.dart';

class TuitionHistoryScreen extends StatefulWidget {
  final int enrollmentId;
  final String courseName;

  const TuitionHistoryScreen({
    super.key,
    required this.enrollmentId,
    required this.courseName,
  });

  @override
  State<TuitionHistoryScreen> createState() => _TuitionHistoryScreenState();
}

class _TuitionHistoryScreenState extends State<TuitionHistoryScreen> {
  late final PagedFetchController<CoursePaymentDto> _controller;

  @override
  void initState() {
    super.initState();
    _controller = PagedFetchController<CoursePaymentDto>(
      fetcher: _fetch,
      pageSize: 20,
    );
  }

  Future<PagedResult<CoursePaymentDto>> _fetch(
    int page,
    int pageSize,
    String search,
  ) {
    return context.read<TuitionPaymentProvider>().history(
      widget.enrollmentId,
      page: page,
      pageSize: pageSize,
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Historija uplata'),
            Text(
              widget.courseName,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.labelSmall,
            ),
          ],
        ),
      ),
      body: PagedListView<CoursePaymentDto>(
        controller: _controller,
        showSearch: false,
        emptyMessage: 'Nema evidentiranih uplata.',
        itemBuilder: (context, payment) => _PaymentRow(payment: payment),
      ),
    );
  }
}

class _PaymentRow extends StatelessWidget {
  final CoursePaymentDto payment;

  const _PaymentRow({required this.payment});

  @override
  Widget build(BuildContext context) {
    final refundedCents = payment.refundedCents ?? 0;
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: ListTile(
        title: Row(
          children: [
            Expanded(
              child: Text(
                formatKM(
                  payment.amountCents / 100,
                  currency: payment.currency,
                ),
              ),
            ),
            StatusChip.payment(payment.status),
          ],
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(_period(payment)),
            if (refundedCents > 0)
              Text(
                _refund(payment),
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
          ],
        ),
      ),
    );
  }

  String _period(CoursePaymentDto payment) {
    final start = payment.periodStart;
    final end = payment.periodEnd;
    if (start != null && end != null) {
      return 'Pristup: ${formatDate(start)} – ${formatDate(end)}';
    }
    final paidAt = payment.paidAt;
    return paidAt == null
        ? 'Pristup nije evidentiran.'
        : 'Plaćeno ${formatDate(paidAt)}';
  }

  String _refund(CoursePaymentDto payment) {
    final parts = <String>[
      'Povrat: '
          '${formatKM((payment.refundedCents ?? 0) / 100, currency: payment.currency)}',
      if (payment.refundedAt != null) formatDate(payment.refundedAt!),
    ];
    return parts.join(' · ');
  }
}
