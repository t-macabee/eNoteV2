import 'package:flutter/material.dart';

Future<bool?> confirmDialog({
  required BuildContext context,
  required String title,
  required String message,
  String confirmText = 'Potvrdi',
  String cancelText = 'Otkaži',
}) {
  return showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (context) => AlertDialog(
      title: Text(title),
      content: Text(message),
      actions: [
        TextButton(
          onPressed: () {
            Navigator.pop(context, false);
          },
          child: Text(cancelText),
        ),
        ElevatedButton(
          onPressed: () {
            Navigator.pop(context, true);
          },
          child: Text(confirmText),
        ),
      ],
    ),
  );
}
