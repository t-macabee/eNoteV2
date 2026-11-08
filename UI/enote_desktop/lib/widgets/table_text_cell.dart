import 'package:flutter/material.dart';

class TableTextCell extends StatelessWidget {
  final String text;
  final double maxWidth;
  final TextStyle? style;

  const TableTextCell({
    super.key,
    required this.text,
    required this.maxWidth,
    this.style,
  });

  @override
  Widget build(BuildContext context) {
    return ConstrainedBox(
      constraints: BoxConstraints(maxWidth: maxWidth),
      child: Tooltip(
        message: text,
        child: Text(
          text,
          style: style,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
        ),
      ),
    );
  }
}
