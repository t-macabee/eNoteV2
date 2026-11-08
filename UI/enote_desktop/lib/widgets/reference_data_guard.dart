import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

Future<bool> ensureReferenceData<T>(
  BuildContext context, {
  required Future<PagedResult<T>> Function() fetchFirstPage,
  required String message,
}) async {
  final PagedResult<T> page;
  try {
    page = await fetchFirstPage();
  } catch (e) {
    if (context.mounted) {
      ErrorBanner.show(context, message: userMessage(e));
    }
    return false;
  }

  if (page.items.isNotEmpty) {
    return true;
  }

  if (context.mounted) {
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }
  return false;
}
