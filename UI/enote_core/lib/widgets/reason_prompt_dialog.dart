import 'package:flutter/material.dart';

Future<String?> promptForReason(
  BuildContext context, {
  required String title,
  int minLength = 1,
}) {
  return showDialog<String>(
    context: context,
    barrierDismissible: false,
    builder: (context) => ReasonPromptDialog(title: title, minLength: minLength),
  );
}

class ReasonPromptDialog extends StatefulWidget {
  final String title;
  final int minLength;

  const ReasonPromptDialog({
    super.key,
    required this.title,
    this.minLength = 1,
  });

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

  String? get _errorText {
    final reason = _controller.text.trim();
    if (reason.isEmpty) {
      return 'Razlog je obavezan.';
    }
    if (reason.length < widget.minLength) {
      return 'Razlog mora imati najmanje ${widget.minLength} karaktera.';
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    final errorText = _errorText;
    return AlertDialog(
      title: Text(widget.title),
      content: TextField(
        controller: _controller,
        autofocus: true,
        maxLines: 3,
        maxLength: 500,
        decoration: InputDecoration(
          labelText: 'Razlog *',
          errorText: errorText,
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
          onPressed: errorText != null
              ? null
              : () => Navigator.of(context).pop(_controller.text.trim()),
          child: const Text('Potvrdi'),
        ),
      ],
    );
  }
}
