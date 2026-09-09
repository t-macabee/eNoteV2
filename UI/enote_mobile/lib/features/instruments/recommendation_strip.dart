import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';
import '../../widgets/section_header.dart';
import '../../widgets/wide_image.dart';

/// Horizontal recommendation strip above the catalogue list (S7).
///
/// Renders nothing at all when there is nothing to recommend: the
/// recommender is an extra, so a failed or empty call must leave the
/// catalogue looking normal rather than showing an error (02 §5, S7).
class RecommendationStrip extends StatelessWidget {
  static const double cardWidth = 160;
  static const double cardHeight = 200;
  static const double imageHeight = 88;

  final List<InstrumentRecommendationDto> recommendations;
  final void Function(InstrumentDto instrument) onOpen;

  const RecommendationStrip({
    super.key,
    required this.recommendations,
    required this.onOpen,
  });

  @override
  Widget build(BuildContext context) {
    if (recommendations.isEmpty) return const SizedBox.shrink();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SectionHeader(title: 'Preporučeno za vas'),
        SizedBox(
          height: cardHeight,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16),
            itemCount: recommendations.length,
            separatorBuilder: (_, _) => const SizedBox(width: 12),
            itemBuilder: (context, index) =>
                _Card(recommendation: recommendations[index], onOpen: onOpen),
          ),
        ),
        const SizedBox(height: 8),
      ],
    );
  }
}

class _Card extends StatelessWidget {
  final InstrumentRecommendationDto recommendation;
  final void Function(InstrumentDto instrument) onOpen;

  const _Card({required this.recommendation, required this.onOpen});

  @override
  Widget build(BuildContext context) {
    final instrument = recommendation.instrument;
    final reasons = recommendation.reasons;
    final extraReasons = reasons.length > 1 ? reasons.length - 1 : 0;

    return SizedBox(
      width: RecommendationStrip.cardWidth,
      height: RecommendationStrip.cardHeight,
      child: Card(
        margin: EdgeInsets.zero,
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: () => onOpen(instrument),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              wideNetworkImage(
                instrument.imagePath,
                context.read<ApiClient>(),
                width: RecommendationStrip.cardWidth,
                height: RecommendationStrip.imageHeight,
              ),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(8, 8, 8, 0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${instrument.manufacturer} ${instrument.model}',
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(context).textTheme.titleSmall,
                      ),
                      if (reasons.isNotEmpty) ...[
                        const SizedBox(height: 2),
                        Text(
                          reasons.first,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: AppTheme.textSecondary,
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              if (extraReasons > 0)
                Align(
                  alignment: Alignment.centerLeft,
                  child: TextButton(
                    onPressed: () => _showReasons(context),
                    style: TextButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 8),
                      minimumSize: const Size(0, 32),
                      tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                    ),
                    child: Text('+$extraReasons razloga'),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  void _showReasons(BuildContext context) {
    final instrument = recommendation.instrument;
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '${instrument.manufacturer} ${instrument.model}',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 12),
              for (final reason in recommendation.reasons)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(
                        Icons.check_circle_outline,
                        size: 18,
                        color: AppTheme.primary,
                      ),
                      const SizedBox(width: 8),
                      Expanded(child: Text(reason)),
                    ],
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
