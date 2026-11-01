import 'package:flutter/material.dart';

class ShellNavigation extends InheritedWidget {
  final void Function(String label) openEntry;

  const ShellNavigation({
    super.key,
    required this.openEntry,
    required super.child,
  });

  static ShellNavigation? maybeOf(BuildContext context) {
    return context.dependOnInheritedWidgetOfExactType<ShellNavigation>();
  }

  static ShellNavigation of(BuildContext context) {
    final navigation = maybeOf(context);
    assert(navigation != null, 'No ShellNavigation found in context');
    return navigation!;
  }

  @override
  bool updateShouldNotify(ShellNavigation oldWidget) {
    return openEntry != oldWidget.openEntry;
  }
}
