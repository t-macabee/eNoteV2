import 'package:enote_core/models/shared/enums.dart';
import 'package:enote_core/models/shared/payment_record.dart';
import '../../formatting/formatters.dart';

class CreateTuitionIntentResponse {
  final int enrollmentId;
  final String paymentIntentId;
  final String clientSecret;
  final int amountCents;
  final String currency;
  final PaymentStatus status;

  CreateTuitionIntentResponse({
    required this.enrollmentId,
    required this.paymentIntentId,
    required this.clientSecret,
    required this.amountCents,
    required this.currency,
    required this.status,
  });

  factory CreateTuitionIntentResponse.fromJson(Map<String, dynamic> json) {
    return CreateTuitionIntentResponse(
      enrollmentId: json['enrollmentId'] as int? ?? 0,
      paymentIntentId: json['paymentIntentId'] as String? ?? '',
      clientSecret: json['clientSecret'] as String? ?? '',
      amountCents: (json['amountCents'] as num?)?.toInt() ?? 0,
      currency: json['currency'] as String? ?? 'bam',
      status: PaymentStatus.fromJson(json['status'] as String?),
    );
  }
}

class CoursePaymentDto implements PaymentRecord {
  final int id;
  final int enrollmentId;
  final String paymentIntentId;
  @override
  final int amountCents;
  @override
  final String currency;
  @override
  final PaymentStatus status;
  @override
  final DateTime? paidAt;
  final DateTime? periodStart;
  final DateTime? periodEnd;

  CoursePaymentDto({
    required this.id,
    required this.enrollmentId,
    required this.paymentIntentId,
    required this.amountCents,
    required this.currency,
    required this.status,
    this.paidAt,
    this.periodStart,
    this.periodEnd,
  });

  factory CoursePaymentDto.fromJson(Map<String, dynamic> json) {
    return CoursePaymentDto(
      id: json['id'] as int? ?? 0,
      enrollmentId: json['enrollmentId'] as int? ?? 0,
      paymentIntentId: json['paymentIntentId'] as String? ?? '',
      amountCents: (json['amountCents'] as num?)?.toInt() ?? 0,
      currency: json['currency'] as String? ?? 'bam',
      status: PaymentStatus.fromJson(json['status'] as String?),
      paidAt: parseDate(json['paidAt']),
      periodStart: parseDate(json['periodStart']),
      periodEnd: parseDate(json['periodEnd']),
    );
  }
}
