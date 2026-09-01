class MusicStoreDto {
  final int id;
  final String storeName;
  final String businessHours;
  final int? addressId;
  final String? addressStreet;
  final String? addressCity;

  MusicStoreDto({
    required this.id,
    required this.storeName,
    required this.businessHours,
    this.addressId,
    this.addressStreet,
    this.addressCity,
  });

  factory MusicStoreDto.fromJson(Map<String, dynamic> json) {
    return MusicStoreDto(
      id: json['id'] as int? ?? 0,
      storeName: json['storeName'] as String? ?? '',
      businessHours: json['businessHours'] as String? ?? '',
      addressId: json['addressId'] as int?,
      addressStreet: json['addressStreet'] as String?,
      addressCity: json['addressCity'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'storeName': storeName,
    'businessHours': businessHours,
    if (addressId != null) 'addressId': addressId,
    if (addressStreet != null) 'addressStreet': addressStreet,
    if (addressCity != null) 'addressCity': addressCity,
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

