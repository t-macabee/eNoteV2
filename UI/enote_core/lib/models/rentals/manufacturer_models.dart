class ManufacturerDto {
  final int id;
  final String name;
  final String? deleteBlockedReason;

  ManufacturerDto({required this.id, required this.name, this.deleteBlockedReason});

  factory ManufacturerDto.fromJson(Map<String, dynamic> json) {
    return ManufacturerDto(
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

class ManufacturerRequest {
  final String name;

  ManufacturerRequest({required this.name});

  Map<String, dynamic> toJson() => {'name': name};
}
