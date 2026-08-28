class MusicStoreDto {
  final int id;
  final String storeName;
  final String businessHours;

  MusicStoreDto({
    required this.id,
    required this.storeName,
    required this.businessHours,
  });

  factory MusicStoreDto.fromJson(Map<String, dynamic> json) {
    return MusicStoreDto(
      id: json['id'] as int? ?? 0,
      storeName: json['storeName'] as String? ?? '',
      businessHours: json['businessHours'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'storeName': storeName,
    'businessHours': businessHours,
  };
}

class MusicStoreRequest {
  final String storeName;
  final String businessHours;

  MusicStoreRequest({required this.storeName, required this.businessHours});

  Map<String, dynamic> toJson() => {
    'storeName': storeName,
    'businessHours': businessHours,
  };
}

