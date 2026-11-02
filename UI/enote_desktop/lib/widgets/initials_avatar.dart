import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

class InitialsAvatar extends StatelessWidget {
  final String text;
  final double radius;

  const InitialsAvatar({super.key, required this.text, this.radius = 28});

  @override
  Widget build(BuildContext context) {
    return CircleAvatar(
      radius: radius,
      backgroundColor: AppTheme.primary,
      child: Text(
        initialsOf(text),
        style: TextStyle(
          color: AppTheme.onPrimary,
          fontWeight: FontWeight.w600,
          fontSize: radius * 0.7,
        ),
      ),
    );
  }
}
