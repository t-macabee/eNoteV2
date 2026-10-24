import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/date_field.dart';

Future<MembershipPaymentDto?> showMembershipPaymentDialog(
  BuildContext context, {
  required MembershipPaymentsApi<dynamic> api,
  required int studentId,
  required DateTime? membershipPaidUntil,
}) {
  return showDialog<MembershipPaymentDto>(
    context: context,
    barrierDismissible: false,
    builder: (_) => _MembershipPaymentDialog(
      api: api,
      studentId: studentId,
      membershipPaidUntil: membershipPaidUntil,
    ),
  );
}

class _MembershipPaymentDialog extends StatefulWidget {
  final MembershipPaymentsApi<dynamic> api;
  final int studentId;
  final DateTime? membershipPaidUntil;

  const _MembershipPaymentDialog({
    required this.api,
    required this.studentId,
    required this.membershipPaidUntil,
  });

  @override
  State<_MembershipPaymentDialog> createState() =>
      _MembershipPaymentDialogState();
}

class _MembershipPaymentDialogState extends State<_MembershipPaymentDialog> {
  final _formKey = GlobalKey<FormState>();
  final _amountController = TextEditingController();
  final _receiptController = TextEditingController();
  final _noteController = TextEditingController();

  DateTime? _periodEnd;
  bool _isSaving = false;

  @override
  void dispose() {
    _amountController.dispose();
    _receiptController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  DateTime get _periodStart {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final paidUntil = widget.membershipPaidUntil;
    if (paidUntil == null) return today;

    final until = DateTime(paidUntil.year, paidUntil.month, paidUntil.day);
    return until.isAfter(today) ? until : today;
  }

  double? _parseAmount() {
    final text = _amountController.text.trim().replaceAll(',', '.');
    return double.tryParse(text);
  }

  String? _validateAmount(String? value) {
    final amount = _parseAmount();
    if (amount == null) {
      return 'Unesite iznos, npr. 50.00.';
    }
    if (amount <= 0) {
      return 'Iznos mora biti veći od 0.';
    }
    if (amount > 99999999.99) {
      return 'Iznos ne smije biti veći od 99999999.99.';
    }
    return null;
  }

  String? _validateReceipt(String? value) {
    final receipt = value?.trim() ?? '';
    if (receipt.isEmpty) {
      return 'Broj potvrde je obavezan.';
    }
    if (receipt.length > 50) {
      return 'Broj potvrde ne smije biti duži od 50 karaktera.';
    }
    return null;
  }

  String? _validatePeriodEnd(DateTime? value) {
    if (value == null) {
      return 'Kraj perioda je obavezan.';
    }
    if (!value.isAfter(_periodStart)) {
      return 'Kraj perioda mora biti nakon početka perioda.';
    }
    return null;
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    setState(() => _isSaving = true);
    try {
      final payment = await widget.api.recordMembershipPayment(
        widget.studentId,
        RecordMembershipPaymentRequest(
          amount: _parseAmount()!,
          periodEnd: _periodEnd,
          receiptNumber: _receiptController.text.trim(),
          note: _noteController.text.trim().isEmpty
              ? null
              : _noteController.text.trim(),
        ),
      );
      if (!mounted) return;
      Navigator.of(context).pop(payment);
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Unesi potvrdu o članarini'),
      content: SizedBox(
        width: 420,
        child: Form(
          key: _formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 16,
              children: [
                Text(
                  'Period počinje ${formatDate(_periodStart)}.',
                  style: const TextStyle(fontSize: 12),
                ),
                TextFormField(
                  controller: _amountController,
                  autofocus: true,
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(
                    labelText: 'Iznos (KM) *',
                    border: OutlineInputBorder(),
                  ),
                  validator: _validateAmount,
                ),
                TextFormField(
                  controller: _receiptController,
                  maxLength: 50,
                  decoration: const InputDecoration(
                    labelText: 'Broj potvrde *',
                    border: OutlineInputBorder(),
                  ),
                  validator: _validateReceipt,
                ),
                DateField(
                  labelText: 'Period do *',
                  firstDate: _periodStart,
                  lastDate: DateTime(
                    DateTime.now().year + 10,
                    DateTime.now().month,
                    DateTime.now().day,
                  ),
                  validator: _validatePeriodEnd,
                  onChanged: (value) => setState(() => _periodEnd = value),
                  autovalidateMode: AutovalidateMode.onUserInteraction,
                ),
                TextFormField(
                  controller: _noteController,
                  maxLines: 2,
                  maxLength: 500,
                  decoration: const InputDecoration(
                    labelText: 'Napomena',
                    border: OutlineInputBorder(),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isSaving ? null : () => Navigator.of(context).pop(),
          child: const Text('Otkaži'),
        ),
        FilledButton(
          onPressed: _isSaving ? null : _save,
          child: _isSaving
              ? const SizedBox(
                  width: 16,
                  height: 16,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Text('Evidentiraj'),
        ),
      ],
    );
  }
}
