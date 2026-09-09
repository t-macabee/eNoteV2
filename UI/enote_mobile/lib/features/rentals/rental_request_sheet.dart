import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/root_shell.dart';
import '../../theme/app_theme.dart';
import 'rental_provider.dart';

/// S9 — the rental request sheet.
///
/// Opens over the instrument detail screen (S8). On a successful create it
/// closes itself, moves the student to the Iznajmljivanja tab and confirms
/// with a snackbar; any refusal from the server is shown verbatim under the
/// button instead (02 §4.4, reason 4).
Future<void> showRentalRequestSheet(
  BuildContext context,
  InstrumentDto instrument,
) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
    ),
    builder: (context) => RentalRequestSheet(instrument: instrument),
  );
}

class RentalRequestSheet extends StatefulWidget {
  /// Mirrors the RSVP note limit; the server itself validates only that the
  /// instrument id is set (02 §5, S9).
  static const int noteMaxLength = 500;

  final InstrumentDto instrument;

  const RentalRequestSheet({super.key, required this.instrument});

  @override
  State<RentalRequestSheet> createState() => _RentalRequestSheetState();
}

class _RentalRequestSheetState extends State<RentalRequestSheet> {
  final TextEditingController _note = TextEditingController();
  bool _isBusy = false;
  String? _errorMessage;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  String get _instrumentName =>
      '${widget.instrument.manufacturer} ${widget.instrument.model}';

  Future<void> _submit() async {
    final rentals = context.read<RentalProvider>();
    final navigator = Navigator.of(context);
    final messenger = ScaffoldMessenger.of(context);

    final confirmed = await confirmDialog(
      context: context,
      title: 'Zahtjev za iznajmljivanje',
      message: 'Poslati zahtjev za $_instrumentName?',
    );
    if (confirmed != true) return;

    setState(() {
      _isBusy = true;
      _errorMessage = null;
    });
    try {
      final note = _note.text.trim();
      await rentals.createRequest(
        RentalCreateRequest(
          instrumentId: widget.instrument.id,
          note: note.isEmpty ? null : note,
        ),
      );
      navigator.pop();
      RootShell.switchTab(2);
      messenger.showSnackBar(
        const SnackBar(content: Text('Zahtjev za iznajmljivanje je poslan.')),
      );
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isBusy = false;
        _errorMessage = userMessage(e);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final error = _errorMessage;
    return Padding(
      padding: EdgeInsets.only(
        bottom: MediaQuery.viewInsetsOf(context).bottom,
      ),
      child: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Zahtjev za iznajmljivanje',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 4),
              Text(
                _instrumentName,
                style: const TextStyle(color: AppTheme.textSecondary),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _note,
                minLines: 3,
                maxLines: 5,
                maxLength: RentalRequestSheet.noteMaxLength,
                decoration: const InputDecoration(labelText: 'Napomena'),
              ),
              const SizedBox(height: 8),
              FilledButton(
                onPressed: _isBusy ? null : _submit,
                child: _isBusy
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Text('Pošalji zahtjev'),
              ),
              if (error != null) ...[
                const SizedBox(height: 12),
                ErrorBanner(message: error),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
