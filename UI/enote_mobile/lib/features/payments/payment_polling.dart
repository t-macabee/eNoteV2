import 'package:enote_core/enote_core.dart';

/// Polls [fetch] up to [attempts] × [interval] until [statusOf] reports
/// `succeeded`.
///
/// Returns the succeeded payment, or the last observation (still pending or
/// `null` when no payment row exists yet) so the caller can show state F
/// instead of a false success.
Future<T?> pollUntilSucceeded<T>(
  Future<T?> Function() fetch,
  PaymentStatus? Function(T) statusOf, {
  int attempts = 5,
  Duration interval = const Duration(seconds: 2),
}) async {
  T? last;
  for (var i = 0; i < attempts; i++) {
    await Future.delayed(interval);
    last = await fetch();
    if (last != null && statusOf(last) == PaymentStatus.succeeded) return last;
  }
  return last;
}
