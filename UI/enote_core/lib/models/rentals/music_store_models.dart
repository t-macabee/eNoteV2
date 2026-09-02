class MusicStoreDto {
  final int id;
  final String storeName;
  final String businessHours;
  final String? phoneNumber;
  final String? imagePath;
  final int? addressId;
  final String? addressStreet;
  final String? addressCity;

  MusicStoreDto({
    required this.id,
    required this.storeName,
    required this.businessHours,
    this.phoneNumber,
    this.imagePath,
    this.addressId,
    this.addressStreet,
    this.addressCity,
  });

  factory MusicStoreDto.fromJson(Map<String, dynamic> json) {
    return MusicStoreDto(
      id: json['id'] as int? ?? 0,
      storeName: json['storeName'] as String? ?? '',
      businessHours: json['businessHours'] as String? ?? '',
      phoneNumber: json['phoneNumber'] as String?,
      imagePath: json['imagePath'] as String?,
      addressId: json['addressId'] as int?,
      addressStreet: json['addressStreet'] as String?,
      addressCity: json['addressCity'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'storeName': storeName,
    'businessHours': businessHours,
    if (phoneNumber != null) 'phoneNumber': phoneNumber,
    if (imagePath != null) 'imagePath': imagePath,
    if (addressId != null) 'addressId': addressId,
    if (addressStreet != null) 'addressStreet': addressStreet,
    if (addressCity != null) 'addressCity': addressCity,
  };
}

class MusicStoreRequest {
  final String storeName;
  final String businessHours;
  final String? phoneNumber;
  final int? addressId;

  MusicStoreRequest({
    required this.storeName,
    required this.businessHours,
    this.phoneNumber,
    this.addressId,
  });

  Map<String, dynamic> toJson() => {
    'storeName': storeName,
    'businessHours': businessHours,
    if (phoneNumber != null) 'phoneNumber': phoneNumber,
    if (addressId != null) 'addressId': addressId,
  };
}

