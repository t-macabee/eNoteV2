class CityDto {
  final int id;
  final String name;

  CityDto({
    required this.id,
    required this.name,
  });

  factory CityDto.fromJson(Map<String, dynamic> json) {
    return CityDto(
      id: json['id'] as int? ?? 0,
      name: json['name'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
  };
}

class CityRequest {
  final String name;

  CityRequest({required this.name});

  Map<String, dynamic> toJson() => {
    'name': name,
  };
}
