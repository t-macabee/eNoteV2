import 'package:flutter/material.dart';

/// Single shared slot for the `enote://stripe-redirect` re-entry.
///
/// Only one payment screen is ever on top, so one field is correct. Both
/// payment screens register/unregister their `recheck` here in
/// `initState`/`dispose` with save/restore stacking; `DeepLinkHandler`
/// invokes whatever is registered.
class StripeRedirect {
  StripeRedirect._();

  static VoidCallback? handler;
}
