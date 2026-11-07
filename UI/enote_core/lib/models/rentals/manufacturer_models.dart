class ManufacturerDto {
  final int id;
  final String name;

  ManufacturerDto({required this.id, required this.name});

  factory ManufacturerDto.fromJson(Map<String, dynamic> json) {
    return ManufacturerDto(
      id: json['id'] as int? ?? 0,
      name: json['name'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {'id': id, 'name': name};
}

class ManufacturerRequest {
  final String name;

  ManufacturerRequest({required this.name});

  Map<String, dynamic> toJson() => {'name': name};
}
