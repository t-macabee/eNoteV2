import 'dart:collection';

import 'package:enote_mobile/features/payments/payment_sheet_gateway.dart';

/// Scripted [PaymentSheetGateway] for widget tests: each [present] consumes
/// the next queued outcome (`null` resolves, an [Exception] is thrown) and
/// defaults to success once the queue is empty.
class FakePaymentSheetGateway implements PaymentSheetGateway {
  final Queue<Object?> _presentScript = Queue<Object?>();
  Exception? initError;

  int initCalls = 0;
  int presentCalls = 0;
  String? lastClientSecret;

  void queuePresent(Object? outcome) => _presentScript.add(outcome);

  @override
  Future<void> init(String clientSecret) async {
    initCalls++;
    lastClientSecret = clientSecret;
    final error = initError;
    if (error != null) throw error;
  }

  @override
  Future<void> present() async {
    presentCalls++;
    final outcome = _presentScript.isNotEmpty
        ? _presentScript.removeFirst()
        : null;
    if (outcome != null) throw outcome;
  }
}
