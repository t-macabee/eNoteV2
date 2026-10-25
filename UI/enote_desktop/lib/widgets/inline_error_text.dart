import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

class InlineErrorText extends StatelessWidget {
  final String message;

  const InlineErrorText(this.message, {super.key});

  @override
  Widget build(BuildContext context) {
    return Text(
      message,
      style: const TextStyle(fontSize: 13, color: AppTheme.error),
    );
  }
}
