import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import 'membership_payment_dialog.dart';

class MembershipSection extends StatefulWidget {
  final MembershipPaymentsApi<dynamic> api;
  final int studentId;
  final DateTime? membershipPaidUntil;
  final ValueChanged<DateTime> onMembershipChanged;

  const MembershipSection({
    super.key,
    required this.api,
    required this.studentId,
    required this.membershipPaidUntil,
    required this.onMembershipChanged,
  });

  @override
  State<MembershipSection> createState() => _MembershipSectionState();
}

class _MembershipSectionState extends State<MembershipSection> {
  static const int _pageSize = 5;

  final List<MembershipPaymentDto> _payments = [];
  int _page = 0;
  bool _hasMore = false;
  bool _isLoading = true;
  bool _isSendingWarning = false;
  bool _hasError = false;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  Future<void> _reload() async {
    setState(() {
      _isLoading = true;
      _hasError = false;
    });

    try {
      final result = await widget.api.getMembershipPayments(
        widget.studentId,
        page: 1,
        pageSize: _pageSize,
      );
      if (!mounted) return;
      setState(() {
        _payments
          ..clear()
          ..addAll(result.items);
        _page = result.page;
        _hasMore = result.hasNext;
      });
    } catch (_) {
      if (mounted) setState(() => _hasError = true);
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _loadMore() async {
    if (_isLoading || !_hasMore) return;

    setState(() => _isLoading = true);
    try {
      final result = await widget.api.getMembershipPayments(
        widget.studentId,
        page: _page + 1,
        pageSize: _pageSize,
      );
      if (!mounted) return;
      setState(() {
        _payments.addAll(result.items);
        _page = result.page;
        _hasMore = result.hasNext;
      });
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _recordPayment() async {
    final payment = await showMembershipPaymentDialog(
      context,
      api: widget.api,
      studentId: widget.studentId,
      membershipPaidUntil: widget.membershipPaidUntil,
    );

    if (payment == null || !mounted) return;

    widget.onMembershipChanged(payment.periodEnd);
    await _reload();
  }

  Future<void> _sendWarning() async {
    setState(() => _isSendingWarning = true);
    try {
      await widget.api.sendMembershipWarning(widget.studentId);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Upozorenje je poslano studentu.')),
      );
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) setState(() => _isSendingWarning = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Članarina',
          style: TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            color: AppTheme.textPrimary,
          ),
        ),
        const SizedBox(height: 8),
        _statusRow(),
        const SizedBox(height: 12),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            FilledButton.icon(
              onPressed: _recordPayment,
              icon: const Icon(Icons.receipt_long_outlined, size: 18),
              label: const Text('Unesi potvrdu'),
            ),
            _warningButton(),
          ],
        ),
        const SizedBox(height: 12),
        _history(),
      ],
    );
  }

  Widget _statusRow() {
    final paidUntil = widget.membershipPaidUntil;
    final isActive = isMembershipActive(paidUntil);

    return Row(
      children: [
        Icon(
          Icons.card_membership_outlined,
          size: 20,
          color: isActive ? AppTheme.success : AppTheme.error,
        ),
        const SizedBox(width: 12),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
          decoration: BoxDecoration(
            color: (isActive ? AppTheme.success : AppTheme.error).withValues(
              alpha: 0.15,
            ),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Text(
            isActive ? 'Aktivna' : 'Istekla / Nema',
            style: TextStyle(
              color: isActive ? AppTheme.success : AppTheme.error,
              fontWeight: FontWeight.w600,
              fontSize: 12,
            ),
          ),
        ),
        if (paidUntil != null) ...[
          const SizedBox(width: 8),
          Text(
            'do ${formatDate(paidUntil)}',
            style: const TextStyle(
              fontSize: 13,
              color: AppTheme.textPrimary,
            ),
          ),
        ],
      ],
    );
  }

  Widget _warningButton() {
    final allowed = isMembershipWarningAllowed(widget.membershipPaidUntil);
    final button = OutlinedButton.icon(
      onPressed: !allowed || _isSendingWarning ? null : _sendWarning,
      icon: _isSendingWarning
          ? const SizedBox(
              width: 16,
              height: 16,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : const Icon(Icons.notifications_active_outlined, size: 18),
      label: const Text('Pošalji upozorenje'),
    );

    if (allowed) return button;

    return Tooltip(
      message:
          'Upozorenje je moguće poslati kada članarina ističe u narednih 7 dana ili je već istekla.',
      child: button,
    );
  }

  Widget _history() {
    if (_hasError) {
      return const Text(
        'Greška pri učitavanju potvrda.',
        style: TextStyle(fontSize: 13, color: AppTheme.error),
      );
    }

    if (_isLoading && _payments.isEmpty) {
      return const SizedBox(
        height: 24,
        child: Center(
          child: SizedBox(
            width: 20,
            height: 20,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
        ),
      );
    }

    if (_payments.isEmpty) {
      return const Text(
        'Nema evidentiranih potvrda.',
        style: TextStyle(fontSize: 13, color: AppTheme.textSecondary),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final payment in _payments)
          ListTile(
            contentPadding: EdgeInsets.zero,
            dense: true,
            leading: const Icon(Icons.receipt_long_outlined),
            title: Text(
              '${formatDate(payment.periodStart)} – ${formatDate(payment.periodEnd)}',
            ),
            subtitle: Text(
              [
                formatKM(payment.amount),
                'Potvrda ${payment.receiptNumber}',
                if (payment.note != null) payment.note!,
              ].join(' · '),
              style: const TextStyle(
                fontSize: 12,
                color: AppTheme.textSecondary,
              ),
            ),
          ),
        if (_hasMore)
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(
              onPressed: _isLoading ? null : _loadMore,
              child: const Text('Učitaj još'),
            ),
          ),
      ],
    );
  }
}
