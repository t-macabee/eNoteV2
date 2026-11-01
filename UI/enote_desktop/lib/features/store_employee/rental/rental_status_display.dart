import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

import '../../../widgets/workflow_actions.dart';

const _actionsByStatus =
    <InstrumentRentalStatus, List<WorkflowAction<RentalTrigger>>>{
      InstrumentRentalStatus.pending: [
        WorkflowAction(
          trigger: RentalTrigger.approve,
          label: 'Odobri',
          icon: Icons.check_circle_outline,
          confirmMessage: 'Odobriti ovaj zahtjev za iznajmljivanje?',
        ),
        WorkflowAction(
          trigger: RentalTrigger.reject,
          label: 'Odbij',
          icon: Icons.cancel_outlined,
          tone: StatusTone.danger,
          requiresNote: true,
        ),
      ],
      InstrumentRentalStatus.approved: [
        WorkflowAction(
          trigger: RentalTrigger.pickup,
          label: 'Preuzeto',
          icon: Icons.inventory_2_outlined,
          confirmMessage: 'Označiti instrument kao preuzet?',
        ),
        WorkflowAction(
          trigger: RentalTrigger.cancel,
          label: 'Otkaži',
          icon: Icons.block,
          tone: StatusTone.danger,
          requiresNote: true,
        ),
      ],
      InstrumentRentalStatus.active: [
        WorkflowAction(
          trigger: RentalTrigger.complete,
          label: 'Završi',
          icon: Icons.done_all,
          confirmMessage: 'Završiti ovo iznajmljivanje?',
        ),
        WorkflowAction(
          trigger: RentalTrigger.returnEarly,
          label: 'Prijevremeni povrat',
          icon: Icons.assignment_return_outlined,
          confirmMessage: 'Označiti instrument kao prijevremeno vraćen?',
        ),
      ],
    };

List<WorkflowAction<RentalTrigger>> rentalTransitionActions(
  InstrumentRentalStatus status, {
  DateTime? dueAt,
}) {
  final actions =
      _actionsByStatus[status] ?? const <WorkflowAction<RentalTrigger>>[];
  if (status != InstrumentRentalStatus.active) return actions;

  final termReached = dueAt != null && !DateTime.now().isBefore(dueAt);
  final allowed = termReached
      ? RentalTrigger.complete
      : RentalTrigger.returnEarly;
  return actions.where((action) => action.trigger == allowed).toList();
}

String rentalTransitionSuccessMessage(RentalTrigger trigger) =>
    switch (trigger) {
      RentalTrigger.approve => 'Zahtjev za iznajmljivanje je odobren.',
      RentalTrigger.reject => 'Zahtjev za iznajmljivanje je odbijen.',
      RentalTrigger.pickup =>
        'Preuzimanje je evidentirano. Iznajmljivanje je aktivno.',
      RentalTrigger.complete =>
        'Povrat je evidentiran. Iznajmljivanje je završeno.',
      RentalTrigger.returnEarly => 'Prijevremeni povrat je evidentiran.',
      RentalTrigger.cancel => 'Iznajmljivanje je otkazano.',
    };
