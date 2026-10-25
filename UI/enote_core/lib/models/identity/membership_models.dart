import '../../formatting/formatters.dart';

class MembershipPaymentDto {
  final int id;
  final int studentId;
  final double amount;
  final DateTime paidAt;
  final DateTime periodStart;
  final DateTime periodEnd;
  final String receiptNumber;
  final String? note;

  MembershipPaymentDto({
    required this.id,
    required this.studentId,
    required this.amount,
    required this.paidAt,
    required this.periodStart,
    required this.periodEnd,
    required this.receiptNumber,
    this.note,
  });

  factory MembershipPaymentDto.fromJson(Map<String, dynamic> json) {
    return MembershipPaymentDto(
      id: json['id'] as int? ?? 0,
      studentId: json['studentId'] as int? ?? 0,
      amount: (json['amount'] as num?)?.toDouble() ?? 0,
      paidAt: parseDate(json['paidAt']) ?? DateTime.now(),
      periodStart: parseDate(json['periodStart']) ?? DateTime.now(),
      periodEnd: parseDate(json['periodEnd']) ?? DateTime.now(),
      receiptNumber: json['receiptNumber'] as String? ?? '',
      note: json['note'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'studentId': studentId,
    'amount': amount,
    'paidAt': paidAt.toIso8601String(),
    'periodStart': periodStart.toIso8601String(),
    'periodEnd': periodEnd.toIso8601String(),
    'receiptNumber': receiptNumber,
    if (note != null) 'note': note,
  };
}

class RecordMembershipPaymentRequest {
  final double amount;
  final DateTime? paidAt;
  final DateTime? periodEnd;
  final int? months;
  final String receiptNumber;
  final String? note;

  RecordMembershipPaymentRequest({
    required this.amount,
    this.paidAt,
    this.periodEnd,
    this.months,
    required this.receiptNumber,
    this.note,
  });

  Map<String, dynamic> toJson() => {
    'amount': amount,
    if (paidAt != null) 'paidAt': paidAt!.toIso8601String(),
    if (periodEnd != null) 'periodEnd': periodEnd!.toIso8601String(),
    if (months != null) 'months': months,
    'receiptNumber': receiptNumber,
    if (note != null) 'note': note,
  };
}

class SendMembershipWarningRequest {
  final String? message;

  SendMembershipWarningRequest({this.message});

  Map<String, dynamic> toJson() => {
    if (message != null) 'message': message,
  };
}
