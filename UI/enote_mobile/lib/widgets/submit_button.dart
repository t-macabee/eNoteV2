import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class SubmitButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final bool isBusy;

  const SubmitButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.isBusy = false,
  });

  @override
  Widget build(BuildContext context) {
    return FilledButton(
      onPressed: isBusy ? null : onPressed,
      child: isBusy
          ? const SizedBox.square(
              dimension: AppSize.spinnerMd,
              child: CircularProgressIndicator(
                strokeWidth: AppSize.spinnerStroke,
              ),
            )
          : Text(label),
    );
  }
}
