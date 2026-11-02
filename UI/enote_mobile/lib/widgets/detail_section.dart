import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import 'section_header.dart';

class DetailSection extends StatelessWidget {
  final String title;
  final Widget child;

  const DetailSection({super.key, required this.title, required this.child});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Divider(height: 1),
        SectionHeader(title: title),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          child: child,
        ),
      ],
    );
  }
}
