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
    return InstrumentRecommendationDto(
      instrument: InstrumentDto.fromJson(
        json['instrument'] as Map<String, dynamic>,
      ),
      score: (json['score'] as num?)?.toDouble() ?? 0.0,
      reasons: (json['reasons'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          const [],
    );
  }
}
