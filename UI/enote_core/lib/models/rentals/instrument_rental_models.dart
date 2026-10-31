import 'package:enote_core/models/shared/enums.dart';
import '../../formatting/formatters.dart';

class InstrumentRentalDto {
  final int id;
  final int instrumentId;
  final int musicStoreId;
  final int studentProfileId;
  final int studentUserId;
  final String? studentName;
  final String instrumentModel;
  final String instrumentType;
  final String storeName;
  final String? instrumentImagePath;
  final bool instrumentIsActive;
  final InstrumentRentalStatus rentalStatus;
  final String? requestNote;
  final String? note;
  final DateTime requestedAt;
  final DateTime? approvedAt;
  final DateTime? rejectedAt;
  final DateTime? pickedUpAt;
  final DateTime? returnedAt;
  final DateTime? pickupDeadline;
  final DateTime? dueAt;
  final DateTime? renewalOpensAt;
  final int? approvedById;
  final int? rejectedById;
  final double fee;
  final double? dailyFee;
  final double dailyPenaltyFee;
  final int? daysCharged;
  final int? overdueDays;
  final double? penaltyFee;
  final double? totalFee;
  final bool isPaid;
  final double? amountPaid;
  final DateTime? paidAt;
  final int? otherPendingRequests;

  InstrumentRentalDto({
    required this.id,
    required this.instrumentId,
    required this.musicStoreId,
    required this.studentProfileId,
    required this.studentUserId,
    this.studentName,
    required this.instrumentModel,
    required this.instrumentType,
    required this.storeName,
    this.instrumentImagePath,
    this.instrumentIsActive = true,
    required this.rentalStatus,
    this.requestNote,
    this.note,
    required this.requestedAt,
    this.approvedAt,
    this.rejectedAt,
    this.pickedUpAt,
    this.returnedAt,
    this.pickupDeadline,
    this.dueAt,
    this.renewalOpensAt,
    this.approvedById,
    this.rejectedById,
    required this.fee,
    this.dailyFee,
    this.dailyPenaltyFee = 0.0,
    this.daysCharged,
    this.overdueDays,
    this.penaltyFee,
    this.totalFee,
    required this.isPaid,
    this.amountPaid,
    this.paidAt,
    this.otherPendingRequests,
  });

  factory InstrumentRentalDto.fromJson(Map<String, dynamic> json) {
    return InstrumentRentalDto(
      id: json['id'] as int? ?? 0,
      instrumentId: json['instrumentId'] as int? ?? 0,
      musicStoreId: json['musicStoreId'] as int? ?? 0,
      studentProfileId: json['studentProfileId'] as int? ?? 0,
      studentUserId: json['studentUserId'] as int? ?? 0,
      studentName: json['studentName'] as String?,
      instrumentModel: json['instrumentModel'] as String? ?? '',
      instrumentType: json['instrumentType'] as String? ?? '',
      storeName: json['storeName'] as String? ?? '',
      instrumentImagePath: json['instrumentImagePath'] as String?,
      instrumentIsActive: json['instrumentIsActive'] as bool? ?? true,
      rentalStatus:
          InstrumentRentalStatus.fromJson(json['rentalStatus'] as String?),
      requestNote: json['requestNote'] as String?,
      note: json['note'] as String?,
      requestedAt: parseDate(json['requestedAt']) ?? DateTime.now(),
      approvedAt: parseDate(json['approvedAt']),
      rejectedAt: parseDate(json['rejectedAt']),
      pickedUpAt: parseDate(json['pickedUpAt']),
      returnedAt: parseDate(json['returnedAt']),
      pickupDeadline: parseDate(json['pickupDeadline']),
      dueAt: parseDate(json['dueAt']),
      renewalOpensAt: parseDate(json['renewalOpensAt']),
      approvedById: json['approvedById'] as int?,
      rejectedById: json['rejectedById'] as int?,
      fee: (json['fee'] as num?)?.toDouble() ?? 0.0,
      dailyFee: (json['dailyFee'] as num?)?.toDouble(),
      dailyPenaltyFee: (json['dailyPenaltyFee'] as num?)?.toDouble() ?? 0.0,
      daysCharged: json['daysCharged'] as int?,
      overdueDays: json['overdueDays'] as int?,
      penaltyFee: (json['penaltyFee'] as num?)?.toDouble(),
      totalFee: (json['totalFee'] as num?)?.toDouble(),
      isPaid: json['isPaid'] as bool? ?? false,
      amountPaid: (json['amountPaid'] as num?)?.toDouble(),
      paidAt: parseDate(json['paidAt']),
      otherPendingRequests: json['otherPendingRequests'] as int?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'instrumentId': instrumentId,
    'musicStoreId': musicStoreId,
    'studentProfileId': studentProfileId,
    'studentUserId': studentUserId,
    if (studentName != null) 'studentName': studentName,
    'instrumentModel': instrumentModel,
    'instrumentType': instrumentType,
    'storeName': storeName,
    if (instrumentImagePath != null) 'instrumentImagePath': instrumentImagePath,
    'instrumentIsActive': instrumentIsActive,
    'rentalStatus': rentalStatus.toJson(),
    if (requestNote != null) 'requestNote': requestNote,
    if (note != null) 'note': note,
    'requestedAt': requestedAt.toIso8601String(),
    if (approvedAt != null) 'approvedAt': approvedAt!.toIso8601String(),
    if (rejectedAt != null) 'rejectedAt': rejectedAt!.toIso8601String(),
    if (pickedUpAt != null) 'pickedUpAt': pickedUpAt!.toIso8601String(),
    if (returnedAt != null) 'returnedAt': returnedAt!.toIso8601String(),
    if (pickupDeadline != null)
      'pickupDeadline': pickupDeadline!.toIso8601String(),
    if (dueAt != null) 'dueAt': dueAt!.toIso8601String(),
    if (renewalOpensAt != null)
      'renewalOpensAt': renewalOpensAt!.toIso8601String(),
    if (approvedById != null) 'approvedById': approvedById,
    if (rejectedById != null) 'rejectedById': rejectedById,
    'fee': fee,
    if (dailyFee != null) 'dailyFee': dailyFee,
    'dailyPenaltyFee': dailyPenaltyFee,
    if (daysCharged != null) 'daysCharged': daysCharged,
    if (overdueDays != null) 'overdueDays': overdueDays,
    if (penaltyFee != null) 'penaltyFee': penaltyFee,
    if (totalFee != null) 'totalFee': totalFee,
    'isPaid': isPaid,
    if (amountPaid != null) 'amountPaid': amountPaid,
    if (paidAt != null) 'paidAt': paidAt!.toIso8601String(),
    if (otherPendingRequests != null)
      'otherPendingRequests': otherPendingRequests,
  };

  String get chargeLabel {
    final days = daysCharged;
    if (days == null) return '—';
    return '$days dana × ${formatKM(dailyFee)}';
  }

  bool get hasOverdueDays => (overdueDays ?? 0) > 0;

  bool get isOverdue =>
      rentalStatus == InstrumentRentalStatus.active && hasOverdueDays;

  bool get hasPenaltyTerms =>
      rentalStatus == InstrumentRentalStatus.approved ||
      rentalStatus == InstrumentRentalStatus.active ||
      rentalStatus == InstrumentRentalStatus.completed ||
      rentalStatus == InstrumentRentalStatus.returnedEarly;

  bool isRenewableAt(DateTime now) =>
      rentalStatus == InstrumentRentalStatus.active &&
      renewalOpensAt != null &&
      dueAt != null &&
      !now.isBefore(renewalOpensAt!) &&
      now.isBefore(dueAt!);
}

class RentalCreateRequest {
  final int instrumentId;
  final String? note;

  RentalCreateRequest({required this.instrumentId, this.note});

  Map<String, dynamic> toJson() => {
    'instrumentId': instrumentId,
    if (note != null) 'note': note,
  };
}

class RentalStatusRequest {
  final String? note;

  RentalStatusRequest({this.note});

  Map<String, dynamic> toJson() => {
    if (note != null) 'note': note,
  };
}

