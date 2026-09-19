import 'package:flutter/widgets.dart';

import 'package:enote_core/enote_core.dart';

mixin FormSubmitState<T extends StatefulWidget> on State<T> {
  bool isSubmitting = false;

  Future<void> submit(Future<void> Function() action) =>
      submitWith((busy) => isSubmitting = busy, action);

  Future<void> submitWith(
    void Function(bool busy) setBusy,
    Future<void> Function() action,
  ) async {
    setState(() => setBusy(true));
    try {
      await action();
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) setState(() => setBusy(false));
    }
  }
}
