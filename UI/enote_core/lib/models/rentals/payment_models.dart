import 'package:enote_core/models/shared/enums.dart';
import '../../formatting/formatters.dart';

class RentalPaymentDto {
  final int id;
  final int rentalId;
  final String paymentIntentId;
  final int amountCents;
  final String currency;
  final PaymentStatus status;
  final DateTime? paidAt;
  final DateTime? refundedAt;
  final int? refundedCents;

  RentalPaymentDto({
    required this.id,
    required this.rentalId,
    required this.paymentIntentId,
    required this.amountCents,
    required this.currency,
    required this.status,
    this.paidAt,
    this.refundedAt,
    this.refundedCents,
  });

  factory RentalPaymentDto.fromJson(Map<String, dynamic> json) {
    return RentalPaymentDto(
      id: json['id'] as int? ?? 0,
      rentalId: json['rentalId'] as int? ?? 0,
      paymentIntentId: json['paymentIntentId'] as String? ?? '',
      amountCents: (json['amountCents'] as num?)?.toInt() ?? 0,
      currency: json['currency'] as String? ?? '',
      status: _parseStatus(json['status']),
      paidAt: parseDate(json['paidAt']),
      refundedAt: parseDate(json['refundedAt']),
      refundedCents: (json['refundedCents'] as num?)?.toInt(),
    );
  }

  static PaymentStatus _parseStatus(dynamic value) {
    if (value is String) return PaymentStatus.fromJson(value);
    if (value is int) {
      return switch (value) {
        1 => PaymentStatus.requiresAction,
        2 => PaymentStatus.succeeded,
        3 => PaymentStatus.failed,
        4 => PaymentStatus.canceled,
        5 => PaymentStatus.refunded,
        6 => PaymentStatus.partiallyRefunded,
        _ => PaymentStatus.requiresAction,
      };
    }
    return PaymentStatus.requiresAction;
  }
}

class CreatePaymentIntentResponse {
  final int rentalId;
  final String paymentIntentId;
  final String clientSecret;
  final int amountCents;
  final String currency;
  final PaymentStatus status;

  CreatePaymentIntentResponse({
    required this.rentalId,
    required this.paymentIntentId,
    required this.clientSecret,
    required this.amountCents,
    required this.currency,
    required this.status,
  });

  factory CreatePaymentIntentResponse.fromJson(Map<String, dynamic> json) {
    return CreatePaymentIntentResponse(
      rentalId: json['rentalId'] as int? ?? 0,
      paymentIntentId: json['paymentIntentId'] as String? ?? '',
      clientSecret: json['clientSecret'] as String? ?? '',
      amountCents: (json['amountCents'] as num?)?.toInt() ?? 0,
      currency: json['currency'] as String? ?? 'eur',
      status: PaymentStatus.fromJson(json['status'] as String? ?? 'RequiresAction'),
    );
  }
}

class RefundRequest {
  final int? amountCents;

  RefundRequest({this.amountCents});

  Map<String, dynamic> toJson() => {
    if (amountCents != null) 'amountCents': amountCents,
  };
}

