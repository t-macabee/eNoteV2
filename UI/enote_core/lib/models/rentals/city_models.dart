class CityDto {
  final int id;
  final String name;
  final String? deleteBlockedReason;

  CityDto({required this.id, required this.name, this.deleteBlockedReason});

  factory CityDto.fromJson(Map<String, dynamic> json) {
    return CityDto(
      id: json['id'] as int? ?? 0,
      name: json['name'] as String? ?? '',
      deleteBlockedReason: json['deleteBlockedReason'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    if (deleteBlockedReason != null) 'deleteBlockedReason': deleteBlockedReason,
  };
}

class CityRequest {
  final String name;

  CityRequest({required this.name});

  Map<String, dynamic> toJson() => {'name': name};
}
