import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';

class RentalTimelineStep {
  final String label;
  final DateTime? at;
  final String? actor;
  final String? note;
  final bool isFilled;
  final bool isTerminalFailure;

  const RentalTimelineStep({
    required this.label,
    this.at,
    this.actor,
    this.note,
    this.isFilled = true,
    this.isTerminalFailure = false,
  });

  String? get actorLine {
    final who = actor;
    if (who == null) return null;
    final text = note?.trim();
    if (text == null || text.isEmpty) return who;
    return '$who · „$text"';
  }
}

class RentalTimeline extends StatelessWidget {
  final InstrumentRentalDto rental;

  const RentalTimeline({super.key, required this.rental});

  static const String _me = 'Vi';

  static List<RentalTimelineStep> stepsFor(InstrumentRentalDto rental) {
    final store = rental.storeName;
    final requested = RentalTimelineStep(
      label: 'Zatraženo',
      at: rental.requestedAt,
      actor: _me,
      note: rental.requestNote,
    );
    final approved = RentalTimelineStep(
      label: 'Odobreno',
      at: rental.approvedAt,
      actor: store,
      note: rental.note,
    );
    final pickedUp = RentalTimelineStep(
      label: 'Preuzeto',
      at: rental.pickedUpAt,
      actor: store,
    );

    RentalTimelineStep payment() => RentalTimelineStep(
      label: 'Plaćanje',
      at: rental.isPaid ? rental.paidAt : null,
      isFilled: rental.isPaid,
    );

    RentalTimelineStep returned(String label) =>
        RentalTimelineStep(label: label, at: rental.returnedAt, actor: store);

    return switch (rental.rentalStatus) {
      InstrumentRentalStatus.pending => [
        requested,
        const RentalTimelineStep(label: 'Odobrenje', isFilled: false),
      ],
      InstrumentRentalStatus.approved => [
        requested,
        approved,
        RentalTimelineStep(
          label: 'Preuzimanje do',
          at: rental.pickupDeadline,
          isFilled: false,
        ),
      ],
      InstrumentRentalStatus.active => [
        requested,
        approved,
        pickedUp,
        RentalTimelineStep(
          label: 'Povrat do',
          at: rental.dueAt,
          isFilled: false,
        ),
      ],
      InstrumentRentalStatus.completed => [
        requested,
        approved,
        pickedUp,
        returned('Vraćeno'),
        payment(),
      ],
      InstrumentRentalStatus.returnedEarly => [
        requested,
        approved,
        pickedUp,
        returned('Vraćeno ranije'),
        payment(),
      ],
      InstrumentRentalStatus.rejected => [
        requested,
        RentalTimelineStep(
          label: 'Odbijeno',
          at: rental.rejectedAt,
          actor: store,
          note: rental.note,
          isTerminalFailure: true,
        ),
      ],
      InstrumentRentalStatus.canceled => [
        requested,
        RentalTimelineStep(
          label: 'Otkazano',
          actor: _me,
          note: rental.note,
          isTerminalFailure: true,
        ),
      ],
    };
  }

  @override
  Widget build(BuildContext context) {
    final steps = stepsFor(rental);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (var i = 0; i < steps.length; i++)
          _StepRow(step: steps[i], isLast: i == steps.length - 1),
      ],
    );
  }
}

class _StepRow extends StatelessWidget {
  static const double _dotSize = 12;
  static const double _lineWidth = 1;

  final RentalTimelineStep step;
  final bool isLast;

  const _StepRow({required this.step, required this.isLast});

  @override
  Widget build(BuildContext context) {
    final Color color = step.isTerminalFailure
        ? AppTheme.error
        : step.isFilled
        ? AppTheme.primary
        : AppTheme.textTertiary;
    final actorLine = step.actorLine;
    final textTheme = Theme.of(context).textTheme;

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Padding(
                padding: const EdgeInsets.only(top: AppSpacing.xxs),
                child: Icon(
                  step.isFilled ? Icons.circle : Icons.circle_outlined,
                  size: _dotSize,
                  color: color,
                ),
              ),
              if (!isLast)
                Expanded(
                  child: Container(
                    width: _lineWidth,
                    margin: const EdgeInsets.symmetric(
                      vertical: AppSpacing.xxs,
                    ),
                    color: AppTheme.outline,
                  ),
                ),
            ],
          ),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(bottom: isLast ? 0 : AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Text(
                          step.label,
                          style: textTheme.labelLarge?.copyWith(
                            color: step.isTerminalFailure
                                ? AppTheme.error
                                : step.isFilled
                                ? AppTheme.textPrimary
                                : AppTheme.textTertiary,
                          ),
                        ),
                      ),
                      Text(
                        step.at == null ? '—' : formatDateTime(step.at!),
                        style: textTheme.bodySmall?.copyWith(
                          color: AppTheme.textSecondary,
                        ),
                      ),
                    ],
                  ),
                  if (actorLine != null) ...[
                    const SizedBox(height: AppSpacing.xxs),
                    Text(
                      actorLine,
                      style: textTheme.bodySmall?.copyWith(
                        color: AppTheme.textSecondary,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
