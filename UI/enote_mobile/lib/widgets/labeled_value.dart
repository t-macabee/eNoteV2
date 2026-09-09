import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class LabeledValue extends StatelessWidget {
  final IconData? icon;
  final String label;
  final String value;

  const LabeledValue({
    super.key,
    this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return InfoRow(
      icon: icon,
      label: label,
      value: value,
      labelWidth: 112,
    );
  }
}
