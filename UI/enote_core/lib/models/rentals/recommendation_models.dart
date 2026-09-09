import 'instrument_models.dart';

class InstrumentRecommendationDto {
  final InstrumentDto instrument;
  final double score;
  final List<String> reasons;

  InstrumentRecommendationDto({
    required this.instrument,
    required this.score,
    required this.reasons,
  });

  factory InstrumentRecommendationDto.fromJson(Map<String, dynamic> json) {
    final instrumentJson = json['instrument'];
    return InstrumentRecommendationDto(
      instrument: instrumentJson is Map
          ? InstrumentDto.fromJson(Map<String, dynamic>.from(instrumentJson))
          : InstrumentDto.fromJson(const {}),
      score: (json['score'] as num?)?.toDouble() ?? 0.0,
      reasons: (json['reasons'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          const [],
    );
  }

  Map<String, dynamic> toJson() => {
        'instrument': instrument.toJson(),
        'score': score,
        'reasons': reasons,
      };
}
