import 'package:enote_core/models/shared/enums.dart';

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
  final InstrumentRentalStatus rentalStatus;
  final String? requestNote;
  final String? note;
  final DateTime requestedAt;
  final DateTime? approvedAt;
  final DateTime? rejectedAt;
  final DateTime? pickedUpAt;
  final DateTime? returnedAt;
  final int? approvedById;
  final int? rejectedById;
  final double fee;
  final double? dailyFee;
  final int? monthsCharged;
  final int? daysCharged;
  final bool isProrated;
  final double? totalFee;
  final bool isPaid;
  final double? amountPaid;
  final DateTime? paidAt;

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
    required this.rentalStatus,
    this.requestNote,
    this.note,
    required this.requestedAt,
    this.approvedAt,
    this.rejectedAt,
    this.pickedUpAt,
    this.returnedAt,
    this.approvedById,
    this.rejectedById,
    required this.fee,
    this.dailyFee,
    this.monthsCharged,
    this.daysCharged,
    required this.isProrated,
    this.totalFee,
    required this.isPaid,
    this.amountPaid,
    this.paidAt,
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
      rentalStatus: _parseStatus(json['rentalStatus']),
      requestNote: json['requestNote'] as String?,
      note: json['note'] as String?,
      requestedAt: _parseDate(json['requestedAt']) ?? DateTime.now(),
      approvedAt: _parseDate(json['approvedAt']),
      rejectedAt: _parseDate(json['rejectedAt']),
      pickedUpAt: _parseDate(json['pickedUpAt']),
      returnedAt: _parseDate(json['returnedAt']),
      approvedById: json['approvedById'] as int?,
      rejectedById: json['rejectedById'] as int?,
      fee: (json['fee'] as num?)?.toDouble() ?? 0.0,
      dailyFee: (json['dailyFee'] as num?)?.toDouble(),
      monthsCharged: json['monthsCharged'] as int?,
      daysCharged: json['daysCharged'] as int?,
      isProrated: json['isProrated'] as bool? ?? false,
      totalFee: (json['totalFee'] as num?)?.toDouble(),
      isPaid: json['isPaid'] as bool? ?? false,
      amountPaid: (json['amountPaid'] as num?)?.toDouble(),
      paidAt: _parseDate(json['paidAt']),
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
    'rentalStatus': rentalStatus.toJson(),
    if (requestNote != null) 'requestNote': requestNote,
    if (note != null) 'note': note,
    'requestedAt': requestedAt.toIso8601String(),
    if (approvedAt != null) 'approvedAt': approvedAt!.toIso8601String(),
    if (rejectedAt != null) 'rejectedAt': rejectedAt!.toIso8601String(),
    if (pickedUpAt != null) 'pickedUpAt': pickedUpAt!.toIso8601String(),
    if (returnedAt != null) 'returnedAt': returnedAt!.toIso8601String(),
    if (approvedById != null) 'approvedById': approvedById,
    if (rejectedById != null) 'rejectedById': rejectedById,
    'fee': fee,
    if (dailyFee != null) 'dailyFee': dailyFee,
    if (monthsCharged != null) 'monthsCharged': monthsCharged,
    if (daysCharged != null) 'daysCharged': daysCharged,
    'isProrated': isProrated,
    if (totalFee != null) 'totalFee': totalFee,
    'isPaid': isPaid,
    if (amountPaid != null) 'amountPaid': amountPaid,
    if (paidAt != null) 'paidAt': paidAt!.toIso8601String(),
  };

  static InstrumentRentalStatus _parseStatus(dynamic value) {
    if (value is int) return InstrumentRentalStatus.fromJson(value);
    return InstrumentRentalStatus.pending;
  }
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

class InstrumentRentalSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;
  final int? instrumentId;
  final InstrumentRentalStatus? rentalStatus;

  InstrumentRentalSearchObject({
    this.page,
    this.pageSize,
    this.includeTotalCount,
    this.instrumentId,
    this.rentalStatus,
  });

  Map<String, dynamic> toQueryMap() => {
    if (page != null) 'page': page,
    if (pageSize != null) 'pageSize': pageSize,
    if (includeTotalCount != null) 'includeTotalCount': includeTotalCount,
    if (instrumentId != null) 'instrumentId': instrumentId,
    if (rentalStatus != null) 'rentalStatus': rentalStatus!.toJson(),
  };
}

DateTime? _parseDate(dynamic value) {
  if (value == null) return null;
  if (value is DateTime) return value;
  if (value is String) {
    try {
      return DateTime.parse(value);
    } catch (_) {
      return null;
    }
  }
  return null;
}
