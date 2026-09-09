import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class MobileFormScaffold extends StatelessWidget {
  final String title;
  final List<Widget> children;
  final String submitLabel;
  final VoidCallback? onSubmit;
  final bool isBusy;
  final String? errorMessage;
  final bool showClose;

  const MobileFormScaffold({
    super.key,
    required this.title,
    required this.children,
    required this.submitLabel,
    required this.onSubmit,
    this.isBusy = false,
    this.errorMessage,
    this.showClose = true,
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(title),
        actions: [
          if (showClose)
            IconButton(
              icon: const Icon(Icons.close),
              onPressed: () => Navigator.of(context).maybePop(),
            ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          ...children,
          const SizedBox(height: 24),
          FilledButton(
            onPressed: isBusy ? null : onSubmit,
            child: isBusy
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Text(submitLabel),
          ),
          if (errorMessage != null) ...[
            const SizedBox(height: 12),
            ErrorBanner(message: errorMessage!),
          ],
        ],
      ),
    );
  }
}
