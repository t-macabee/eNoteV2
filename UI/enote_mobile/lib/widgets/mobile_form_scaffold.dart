import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import 'submit_button.dart';

class MobileFormScaffold extends StatelessWidget {
  final String title;
  final List<Widget> children;
  final String submitLabel;
  final VoidCallback? onSubmit;
  final bool isBusy;
  final String? errorMessage;

  const MobileFormScaffold({
    super.key,
    required this.title,
    required this.children,
    required this.submitLabel,
    required this.onSubmit,
    this.isBusy = false,
    this.errorMessage,
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: ListView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
          ...children,
          const SizedBox(height: AppSpacing.xl),
          SubmitButton(label: submitLabel, onPressed: onSubmit, isBusy: isBusy),
          if (errorMessage != null) ...[
            const SizedBox(height: AppSpacing.md),
            ErrorBanner(message: errorMessage!),
          ],
        ],
      ),
    );
  }
}
