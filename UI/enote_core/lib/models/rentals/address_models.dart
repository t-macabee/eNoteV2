class AddressReferenceDto {
  final int id;
  final String city;
  final String street;
  final String number;

  AddressReferenceDto({
    required this.id,
    required this.city,
    required this.street,
    required this.number,
  });

  factory AddressReferenceDto.fromJson(Map<String, dynamic> json) {
    return AddressReferenceDto(
      id: json['id'] as int? ?? 0,
      city: json['city'] as String? ?? '',
      street: json['street'] as String? ?? '',
      number: json['number'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'city': city,
    'street': street,
    'number': number,
  };
}

class AddressRequest {
  final String city;
  final String street;
  final String number;

  AddressRequest({
    required this.city,
    required this.street,
    required this.number,
  });

  Map<String, dynamic> toJson() => {
    'city': city,
    'street': street,
    'number': number,
  };
}

