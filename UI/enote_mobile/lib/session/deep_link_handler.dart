import 'package:flutter/material.dart';

import '../features/payments/payment_screen.dart';
import '../shell/app_router.dart';

sealed class DeepLinkTarget {
  const DeepLinkTarget();
}

class ResetPasswordTarget extends DeepLinkTarget {
  final String email;
  final String token;

  const ResetPasswordTarget({required this.email, required this.token});
}

class StripeRedirectTarget extends DeepLinkTarget {
  const StripeRedirectTarget();
}

class UnknownTarget extends DeepLinkTarget {
  const UnknownTarget();
}

class DeepLinkHandler {
  DeepLinkTarget? pendingTarget;

  static DeepLinkTarget parse(Uri uri) {
    if (uri.scheme != 'enote') {
      return const UnknownTarget();
    }
    if (uri.host == 'reset-password') {
      final email = uri.queryParameters['email'];
      final token = uri.queryParameters['token'];
      if (email == null ||
          email.isEmpty ||
          token == null ||
          token.isEmpty) {
        return const UnknownTarget();
      }
      return ResetPasswordTarget(email: email, token: token);
    }
    if (uri.host == 'stripe-redirect') {
      return const StripeRedirectTarget();
    }
    return const UnknownTarget();
  }

  void onLink(Uri uri, GlobalKey<NavigatorState> navigatorKey) {
    final target = parse(uri);
    if (navigatorKey.currentState == null) {
      pendingTarget = target;
      return;
    }
    dispatch(navigatorKey, target);
  }

  void flush(GlobalKey<NavigatorState> navigatorKey) {
    final target = pendingTarget;
    pendingTarget = null;
    if (target != null) {
      dispatch(navigatorKey, target);
    }
  }

  static void dispatch(
    GlobalKey<NavigatorState> navigatorKey,
    DeepLinkTarget target,
  ) {
    switch (target) {
      case ResetPasswordTarget(:final email, :final token):
        navigatorKey.currentState?.pushNamedAndRemoveUntil(
          AppRouter.resetPassword,
          (route) => route.isFirst,
          arguments: ResetArgs(email: email, token: token),
        );
      case StripeRedirectTarget():
        // A redirect-based Stripe method coming back to the app: if a
        // payment screen is on top it runs one more poll cycle, else
        // the link is ignored (01 §5.4 step 5).
        PaymentScreen.stripeRedirectHandler?.call();
        break;
      case UnknownTarget():
        break;
    }
  }
}
