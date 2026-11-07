class InstrumentDto {
  final int id;
  final String model;
  final int manufacturerId;
  final String manufacturerName;
  final String? description;
  final String? imagePath;
  final int instrumentTypeId;
  final String instrumentType;
  final double monthlyFee;
  final String musicStore;
  final bool isAvailable;

  InstrumentDto({
    required this.id,
    required this.model,
    required this.manufacturerId,
    required this.manufacturerName,
    this.description,
    this.imagePath,
    required this.instrumentTypeId,
    required this.instrumentType,
    required this.monthlyFee,
    required this.musicStore,
    required this.isAvailable,
  });

  factory InstrumentDto.fromJson(Map<String, dynamic> json) {
    return InstrumentDto(
      id: json['id'] as int? ?? 0,
      model: json['model'] as String? ?? '',
      manufacturerId: json['manufacturerId'] as int? ?? 0,
      manufacturerName: json['manufacturerName'] as String? ?? '',
      description: json['description'] as String?,
      imagePath: json['imagePath'] as String?,
      instrumentTypeId: json['instrumentTypeId'] as int? ?? 0,
      instrumentType: json['instrumentType'] as String? ?? '',
      monthlyFee: (json['monthlyFee'] as num?)?.toDouble() ?? 0.0,
      musicStore: json['musicStore'] as String? ?? '',
      isAvailable: json['isAvailable'] as bool? ?? false,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'model': model,
    'manufacturerId': manufacturerId,
    'manufacturerName': manufacturerName,
    if (description != null) 'description': description,
    if (imagePath != null) 'imagePath': imagePath,
    'instrumentTypeId': instrumentTypeId,
    'instrumentType': instrumentType,
    'monthlyFee': monthlyFee,
    'musicStore': musicStore,
    'isAvailable': isAvailable,
  };
}

class InstrumentRequest {
  final String model;
  final int manufacturerId;
  final String? description;
  final int instrumentTypeId;

  InstrumentRequest({
    required this.model,
    required this.manufacturerId,
    this.description,
    required this.instrumentTypeId,
  });

  Map<String, dynamic> toJson() {
    final normalizedDescription = description?.trim();
    return {
      'model': model,
      'manufacturerId': manufacturerId,
      'description':
          normalizedDescription == null || normalizedDescription.isEmpty
          ? null
          : normalizedDescription,
      'instrumentTypeId': instrumentTypeId,
    };
  }
}
