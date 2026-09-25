import 'package:flutter/material.dart';

Future<String?> promptForReason(BuildContext context, {required String title}) {
  return showDialog<String>(
    context: context,
    barrierDismissible: false,
    builder: (context) => ReasonPromptDialog(title: title),
  );
}

class ReasonPromptDialog extends StatefulWidget {
  final String title;

  const ReasonPromptDialog({super.key, required this.title});

  @override
  State<ReasonPromptDialog> createState() => _ReasonPromptDialogState();
}

class _ReasonPromptDialogState extends State<ReasonPromptDialog> {
  late final TextEditingController _controller;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isEmpty = _controller.text.trim().isEmpty;
    return AlertDialog(
      title: Text(widget.title),
      content: TextField(
        controller: _controller,
        autofocus: true,
        maxLines: 3,
        maxLength: 500,
        decoration: InputDecoration(
          labelText: 'Razlog *',
          errorText: isEmpty ? 'Razlog je obavezan.' : null,
          border: const OutlineInputBorder(),
        ),
        onChanged: (_) => setState(() {}),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Otkaži'),
        ),
        ElevatedButton(
          onPressed: isEmpty
              ? null
              : () => Navigator.of(context).pop(_controller.text.trim()),
          child: const Text('Potvrdi'),
        ),
      ],
    );
  }
}
