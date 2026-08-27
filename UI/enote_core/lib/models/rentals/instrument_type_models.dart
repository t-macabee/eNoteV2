class InstrumentTypeDto {
  final int id;
  final String type;
  final double monthlyFee;

  InstrumentTypeDto({
    required this.id,
    required this.type,
    required this.monthlyFee,
  });

  factory InstrumentTypeDto.fromJson(Map<String, dynamic> json) {
    return InstrumentTypeDto(
      id: json['id'] as int? ?? 0,
      type: json['type'] as String? ?? '',
      monthlyFee: (json['monthlyFee'] as num?)?.toDouble() ?? 0.0,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'type': type,
    'monthlyFee': monthlyFee,
  };
}

class InstrumentTypeRequest {
  final String type;
  final double monthlyFee;

  InstrumentTypeRequest({required this.type, required this.monthlyFee});

  Map<String, dynamic> toJson() => {
    'type': type,
    'monthlyFee': monthlyFee,
  };
}

class InstrumentTypeSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;
  final String? type;

  InstrumentTypeSearchObject({
    this.page,
    this.pageSize,
    this.includeTotalCount,
    this.type,
  });

  Map<String, dynamic> toQueryMap() => {
    if (page != null) 'page': page,
    if (pageSize != null) 'pageSize': pageSize,
    if (includeTotalCount != null) 'includeTotalCount': includeTotalCount,
    if (type != null) 'type': type,
  };
}
