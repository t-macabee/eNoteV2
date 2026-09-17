import 'package:flutter/material.dart';

/// Returns the confirmed refund amount in KM, or `null` when cancelled.
/// An empty input confirms a full refund, reported as `0`.
///
/// When [maxAmountCents] is given, a positive input above the remaining
/// refundable balance is rejected inline instead of failing server-side.
Future<double?> showRefundAmountDialog(
  BuildContext context, {
  int? maxAmountCents,
}) =>
    showDialog<double>(
      context: context,
      barrierDismissible: false,
      builder: (_) => _RefundAmountDialog(maxAmountCents: maxAmountCents),
    );

/// Owns the text controller so it is disposed with the route, after the
/// exit transition — disposing it in the button handlers before the pop left
/// the still-mounted [TextField] on a dead listenable.
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
            ? 'Preostali iznos za povrat je ${(maxAmountCents / 100).toStringAsFixed(2)} KM.'
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
