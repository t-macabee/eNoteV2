import 'package:flutter/material.dart';

Future<String?> showRefundAmountDialog(BuildContext context) {
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
