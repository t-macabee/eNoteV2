import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

Future<double?> showRefundAmountDialog(
  BuildContext context, {
  int? maxAmountCents,
}) =>
    showDialog<double>(
      context: context,
      barrierDismissible: false,
      builder: (_) => _RefundAmountDialog(maxAmountCents: maxAmountCents),
    );

class _RefundAmountDialog extends StatefulWidget {
  final int? maxAmountCents;

  const _RefundAmountDialog({this.maxAmountCents});

  @override
  State<_RefundAmountDialog> createState() => _RefundAmountDialogState();
}

class _RefundAmountDialogState extends State<_RefundAmountDialog> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final maxAmountCents = widget.maxAmountCents;
    final text = _controller.text.trim();
    final hasValue = text.isNotEmpty;
    final parsed = hasValue ? double.tryParse(text.replaceAll(',', '.')) : null;
    final overLimit = maxAmountCents != null &&
        parsed != null &&
        parsed > 0 &&
        (parsed * 100).round() > maxAmountCents;
    final invalid = hasValue && (parsed == null || parsed <= 0 || overLimit);
    final errorText = !invalid
        ? null
        : overLimit
            ? 'Preostali iznos za povrat je ${formatKM(maxAmountCents / 100)}.'
            : 'Unesite važeći iznos.';
    return AlertDialog(
      title: const Text('Refundiraj'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Text('Unesite iznos za djelomični povrat. '
              'Ostavite prazno za puni povrat.'),
          const SizedBox(height: 12),
          TextField(
            controller: _controller,
            autofocus: true,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: InputDecoration(
              labelText: 'Iznos (KM)',
              hintText: 'Prazno = puni povrat',
              errorText: errorText,
              border: const OutlineInputBorder(),
            ),
            onChanged: (_) => setState(() {}),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Otkaži'),
        ),
        ElevatedButton(
          onPressed:
              invalid ? null : () => Navigator.pop(context, parsed ?? 0.0),
          child: const Text('Potvrdi'),
        ),
      ],
    );
  }
}
