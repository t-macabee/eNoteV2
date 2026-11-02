import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';
import '../../widgets/app_sheet_body.dart';
import '../../widgets/section_header.dart';
import '../../widgets/wide_image.dart';

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
            padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
            itemCount: recommendations.length,
            separatorBuilder: (_, _) => const SizedBox(width: AppSpacing.md),
            itemBuilder: (context, index) =>
                _Card(recommendation: recommendations[index], onOpen: onOpen),
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
      ],
    );
  }
}

class _Card extends StatelessWidget {
  static const double reasonIconSize = 18;
  static const double reasonButtonHeight = 32;

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
                  padding: const EdgeInsets.fromLTRB(
                    AppSpacing.sm,
                    AppSpacing.sm,
                    AppSpacing.sm,
                    0,
                  ),
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
                        const SizedBox(height: AppSpacing.xxs),
                        Text(
                          reasons.first,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: AppTheme.textSecondary),
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
                      padding: const EdgeInsets.symmetric(
                        horizontal: AppSpacing.sm,
                      ),
                      minimumSize: const Size(0, _Card.reasonButtonHeight),
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
      builder: (context) => AppSheetBody(
        title: '${instrument.manufacturer} ${instrument.model}',
        children: [
          for (final reason in recommendation.reasons)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(
                    Icons.check_circle_outline,
                    size: _Card.reasonIconSize,
                    color: AppTheme.primary,
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(child: Text(reason)),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
