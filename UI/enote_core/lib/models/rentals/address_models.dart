class AddressReferenceDto {
  final int id;
  final int cityId;
  final String city;
  final String street;
  final String number;
  final String? deleteBlockedReason;

  AddressReferenceDto({
    required this.id,
    required this.cityId,
    required this.city,
    required this.street,
    required this.number,
    this.deleteBlockedReason,
  });

  factory AddressReferenceDto.fromJson(Map<String, dynamic> json) {
    return AddressReferenceDto(
      id: json['id'] as int? ?? 0,
      cityId: json['cityId'] as int? ?? 0,
      city: json['city'] as String? ?? '',
      street: json['street'] as String? ?? '',
      number: json['number'] as String? ?? '',
      deleteBlockedReason: json['deleteBlockedReason'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'cityId': cityId,
    'city': city,
    'street': street,
    'number': number,
    if (deleteBlockedReason != null) 'deleteBlockedReason': deleteBlockedReason,
  };
}

class AddressRequest {
  final int cityId;
  final String street;
  final String number;

  AddressRequest({
    required this.cityId,
    required this.street,
    required this.number,
  });

  Map<String, dynamic> toJson() => {
    'cityId': cityId,
    'street': street,
    'number': number,
  };
}
