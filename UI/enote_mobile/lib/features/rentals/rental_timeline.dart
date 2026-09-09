import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';

/// One row of the rental timeline.
///
/// [isFilled] is what separates a step that has happened from one that is
/// still expected: filled steps carry a timestamp and an actor, hollow ones
/// only name what comes next.
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

  /// `Vi · "note"` / `Muzika d.o.o.` / null when the step has no actor.
  String? get actorLine {
    final who = actor;
    if (who == null) return null;
    final text = note?.trim();
    if (text == null || text.isEmpty) return who;
    return '$who · „$text"';
  }
}

/// S11's *Vremenska linija*, built from [InstrumentRentalDto] alone.
///
/// The DTO carries no actor names, only the approver's and rejecter's user
/// ids, so every store-side step names the **store** instead — never a
/// person and never a raw id (01 §11.1 item 4).
class RentalTimeline extends StatelessWidget {
  final InstrumentRentalDto rental;

  const RentalTimeline({super.key, required this.rental});

  static const String _me = 'Vi';

  /// The steps for [rental]'s current status, in the order of 02 §4.2.
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

    RentalTimelineStep returned(String label) => RentalTimelineStep(
      label: label,
      at: rental.returnedAt,
      actor: store,
    );

    return switch (rental.rentalStatus) {
      InstrumentRentalStatus.pending => [
        requested,
        const RentalTimelineStep(label: 'Odobrenje', isFilled: false),
      ],
      InstrumentRentalStatus.approved => [
        requested,
        approved,
        const RentalTimelineStep(label: 'Preuzimanje', isFilled: false),
      ],
      InstrumentRentalStatus.active => [
        requested,
        approved,
        pickedUp,
        const RentalTimelineStep(label: 'Povrat', isFilled: false),
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
      // The DTO has no `canceledAt`, so this step carries no timestamp.
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

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Padding(
                padding: const EdgeInsets.only(top: 2),
                child: Icon(
                  step.isFilled ? Icons.circle : Icons.circle_outlined,
                  size: 12,
                  color: color,
                ),
              ),
              if (!isLast)
                Expanded(
                  child: Container(
                    width: 1,
                    margin: const EdgeInsets.symmetric(vertical: 2),
                    color: AppTheme.outline,
                  ),
                ),
            ],
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(bottom: isLast ? 0 : 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Text(
                          step.label,
                          style: TextStyle(
                            color: step.isTerminalFailure
                                ? AppTheme.error
                                : step.isFilled
                                ? AppTheme.textPrimary
                                : AppTheme.textTertiary,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                      Text(
                        step.at == null ? '—' : formatDateTime(step.at!),
                        style: const TextStyle(
                          color: AppTheme.textSecondary,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                  if (actorLine != null) ...[
                    const SizedBox(height: 2),
                    Text(
                      actorLine,
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
        ],
      ),
    );
  }
}
