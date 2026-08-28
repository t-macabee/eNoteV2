import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

/// One button per valid transition off the current [InstrumentRentalStatus],
/// table-driven off the same (status → trigger) pairs
/// `InstrumentRental.Transition`'s state machine defines for the
/// StoreEmployee actor — approve/reject/pickup/complete/return-early.
/// `cancel` is a student-only trigger, so it has no button here. Reused by
/// every rental detail screen instead of a bespoke button set per screen.
class RentalTransitionActionRow extends StatefulWidget {
  final InstrumentRentalStatus status;
  final Future<void> Function(RentalTrigger trigger, {String? note}) onTransition;
  final bool enabled;

  const RentalTransitionActionRow({
    super.key,
    required this.status,
    required this.onTransition,
    this.enabled = true,
  });

  @override
  State<RentalTransitionActionRow> createState() => _RentalTransitionActionRowState();
}

class _TransitionAction {
  final RentalTrigger trigger;
  final String label;
  final IconData icon;
  final Color? color;
  final bool requiresNote;
  final String confirmMessage;

  const _TransitionAction({
    required this.trigger,
    required this.label,
    required this.icon,
    required this.confirmMessage,
    this.color,
    this.requiresNote = false,
  });
}

// Mirrors eNote.Domain's InstrumentRental.CreateTransitions() for the
// StoreEmployee actor (eNote/eNote.Domain/Entities/Rentals/InstrumentRental.cs).
const _actionsByStatus = <InstrumentRentalStatus, List<_TransitionAction>>{
  InstrumentRentalStatus.pending: [
    _TransitionAction(
      trigger: RentalTrigger.approve,
      label: 'Odobri',
      icon: Icons.check_circle_outline,
      confirmMessage: 'Odobriti ovaj zahtjev za iznajmljivanje?',
    ),
    _TransitionAction(
      trigger: RentalTrigger.reject,
      label: 'Odbij',
      icon: Icons.cancel_outlined,
      color: Colors.red,
      requiresNote: true,
      confirmMessage: 'Odbiti ovaj zahtjev za iznajmljivanje?',
    ),
  ],
  InstrumentRentalStatus.approved: [
    _TransitionAction(
      trigger: RentalTrigger.pickup,
      label: 'Preuzeto',
      icon: Icons.inventory_2_outlined,
      confirmMessage: 'Označiti instrument kao preuzet?',
    ),
  ],
  InstrumentRentalStatus.active: [
    _TransitionAction(
      trigger: RentalTrigger.complete,
      label: 'Završi',
      icon: Icons.done_all,
      confirmMessage: 'Završiti ovo iznajmljivanje?',
    ),
    _TransitionAction(
      trigger: RentalTrigger.returnEarly,
      label: 'Prijevremeni povrat',
      icon: Icons.assignment_return_outlined,
      confirmMessage: 'Označiti instrument kao prijevremeno vraćen?',
    ),
  ],
};

class _RentalTransitionActionRowState extends State<RentalTransitionActionRow> {
  RentalTrigger? _busyTrigger;

  Future<void> _run(_TransitionAction action) async {
    String? note;
    if (action.requiresNote) {
      note = await _promptForNote(action.label);
      if (note == null) return; // dialog cancelled or empty
    } else {
      final confirmed = await confirmDialog(
        context: context,
        title: action.label,
        message: action.confirmMessage,
      );
      if (confirmed != true) return;
    }

    setState(() => _busyTrigger = action.trigger);
    try {
      await widget.onTransition(action.trigger, note: note);
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _busyTrigger = null);
      }
    }
  }

  Future<String?> _promptForNote(String actionLabel) {
    final noteController = TextEditingController();
    return showDialog<String>(
      context: context,
      barrierDismissible: false,
      builder: (context) => StatefulBuilder(builder: (ctx, setLocal) {
        final isEmpty = noteController.text.trim().isEmpty;
        return AlertDialog(
          title: Text(actionLabel),
          content: TextField(
            controller: noteController,
            autofocus: true,
            maxLines: 3,
            decoration: InputDecoration(
              labelText: 'Razlog *',
              errorText: isEmpty ? 'Razlog je obavezan.' : null,
              border: const OutlineInputBorder(),
            ),
            onChanged: (_) => setLocal(() {}),
          ),
          actions: [
            TextButton(
              onPressed: () {
                noteController.dispose();
                Navigator.pop(context);
              },
              child: const Text('Otkaži'),
            ),
            ElevatedButton(
              onPressed: isEmpty
                  ? null
                  : () {
                      final text = noteController.text.trim();
                      noteController.dispose();
                      Navigator.pop(context, text);
                    },
              child: const Text('Potvrdi'),
            ),
          ],
        );
      }),
    );
  }

  @override
  Widget build(BuildContext context) {
    final actions = _actionsByStatus[widget.status] ?? const [];
    if (actions.isEmpty) return const SizedBox.shrink();

    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: actions.map((action) {
        final isBusy = _busyTrigger == action.trigger;
        return ElevatedButton.icon(
          onPressed: widget.enabled && _busyTrigger == null ? () => _run(action) : null,
          style: action.color != null
              ? ElevatedButton.styleFrom(foregroundColor: action.color)
              : null,
          icon: isBusy
              ? const SizedBox(
                  width: 16,
                  height: 16,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : Icon(action.icon),
          label: Text(action.label),
        );
      }).toList(),
    );
  }
}
