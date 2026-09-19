import 'package:flutter/widgets.dart';

import 'package:enote_core/enote_core.dart';

mixin FormSubmitState<T extends StatefulWidget> on State<T> {
  bool isSubmitting = false;
  String? submitError;

  Future<void> submit(Future<void> Function() action) =>
      submitWith((busy) => isSubmitting = busy, action);

  Future<void> submitWith(
    void Function(bool busy) setBusy,
    Future<void> Function() action, {
    bool showBanner = false,
  }) async {
    setState(() {
      setBusy(true);
      submitError = null;
    });
    try {
      await action();
    } catch (e) {
      if (mounted) {
        final message = userMessage(e);
        if (showBanner) {
          ErrorBanner.show(context, message: message);
        } else {
          setState(() => submitError = message);
        }
      }
    } finally {
      if (mounted) setState(() => setBusy(false));
    }
  }
}
