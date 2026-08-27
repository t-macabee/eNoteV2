class InstrumentDto {
  final int id;
  final String model;
  final String manufacturer;
  final String? description;
  final String? imagePath;
  final int instrumentTypeId;
  final String instrumentType;
  final String musicStore;
  final bool isAvailable;

  InstrumentDto({
    required this.id,
    required this.model,
    required this.manufacturer,
    this.description,
    this.imagePath,
    required this.instrumentTypeId,
    required this.instrumentType,
    required this.musicStore,
    required this.isAvailable,
  });

  factory InstrumentDto.fromJson(Map<String, dynamic> json) {
    return InstrumentDto(
      id: json['id'] as int? ?? 0,
      model: json['model'] as String? ?? '',
      manufacturer: json['manufacturer'] as String? ?? '',
      description: json['description'] as String?,
      imagePath: json['imagePath'] as String?,
      instrumentTypeId: json['instrumentTypeId'] as int? ?? 0,
      instrumentType: json['instrumentType'] as String? ?? '',
      musicStore: json['musicStore'] as String? ?? '',
      isAvailable: json['isAvailable'] as bool? ?? false,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'model': model,
    'manufacturer': manufacturer,
    if (description != null) 'description': description,
    if (imagePath != null) 'imagePath': imagePath,
    'instrumentTypeId': instrumentTypeId,
    'instrumentType': instrumentType,
    'musicStore': musicStore,
    'isAvailable': isAvailable,
  };
}

class InstrumentCreateRequest {
  final String model;
  final String manufacturer;
  final String? description;
  final String? imagePath;
  final int instrumentTypeId;

  InstrumentCreateRequest({
    required this.model,
    required this.manufacturer,
    this.description,
    this.imagePath,
    required this.instrumentTypeId,
  });

  Map<String, dynamic> toJson() => {
    'model': model,
    'manufacturer': manufacturer,
    if (description != null) 'description': description,
    if (imagePath != null) 'imagePath': imagePath,
    'instrumentTypeId': instrumentTypeId,
  };
}

class InstrumentUpdateRequest {
  final String? model;
  final String? manufacturer;
  final String? description;
  final String? imagePath;
  final int? instrumentTypeId;

  InstrumentUpdateRequest({
    this.model,
    this.manufacturer,
    this.description,
    this.imagePath,
    this.instrumentTypeId,
  });

  Map<String, dynamic> toJson() => {
    if (model != null) 'model': model,
    if (manufacturer != null) 'manufacturer': manufacturer,
    if (description != null) 'description': description,
    if (imagePath != null) 'imagePath': imagePath,
    if (instrumentTypeId != null) 'instrumentTypeId': instrumentTypeId,
  };
}

class InstrumentSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;
  final String? model;
  final String? manufacturer;
  final int? instrumentTypeId;
  final bool? isAvailable;

  InstrumentSearchObject({
    this.page,
    this.pageSize,
    this.includeTotalCount,
    this.model,
    this.manufacturer,
    this.instrumentTypeId,
    this.isAvailable,
  });

  Map<String, dynamic> toQueryMap() => {
    if (page != null) 'page': page,
    if (pageSize != null) 'pageSize': pageSize,
    if (includeTotalCount != null) 'includeTotalCount': includeTotalCount,
    if (model != null) 'model': model,
    if (manufacturer != null) 'manufacturer': manufacturer,
    if (instrumentTypeId != null) 'instrumentTypeId': instrumentTypeId,
    if (isAvailable != null) 'isAvailable': isAvailable,
  };
}
