import 'package:flutter/widgets.dart';

mixin FormControllerLifecycle<T extends StatefulWidget> on State<T> {
  final List<TextEditingController> _ownedTextControllers = [];

  TextEditingController textController({String? text}) {
    final controller = TextEditingController(text: text);
    _ownedTextControllers.add(controller);
    return controller;
  }

  void clearTextControllers() {
    for (final controller in _ownedTextControllers) {
      controller.clear();
    }
  }

  @override
  void dispose() {
    for (final controller in _ownedTextControllers) {
      controller.dispose();
    }
    super.dispose();
  }
}
