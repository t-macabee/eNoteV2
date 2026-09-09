class RentalDebtDto {
  final bool hasUnpaidDebt;
  final int? rentalId;

  RentalDebtDto({required this.hasUnpaidDebt, this.rentalId});

  factory RentalDebtDto.fromJson(Map<String, dynamic> json) {
    return RentalDebtDto(
      hasUnpaidDebt: json['hasUnpaidDebt'] as bool? ?? false,
      rentalId: json['rentalId'] as int?,
    );
  }

  Map<String, dynamic> toJson() => {
        'hasUnpaidDebt': hasUnpaidDebt,
        if (rentalId != null) 'rentalId': rentalId,
      };
}
