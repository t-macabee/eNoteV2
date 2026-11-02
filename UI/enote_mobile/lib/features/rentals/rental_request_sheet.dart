import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../widgets/app_sheet_body.dart';
import '../../widgets/app_text_field.dart';
import '../../widgets/form_submit_state.dart';
import '../../widgets/submit_button.dart';
import 'rental_provider.dart';

String rentalRequestSentMessage(int? otherPendingRequests) {
  const sent = 'Zahtjev za iznajmljivanje je poslan.';
  if (otherPendingRequests == null || otherPendingRequests <= 0) return sent;
  return '$sent Broj zahtjeva na čekanju za isti instrument: '
      '$otherPendingRequests.';
}

Future<void> showRentalRequestSheet(
  BuildContext context,
  InstrumentDto instrument,
) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (context) => RentalRequestSheet(instrument: instrument),
  );
}

class RentalRequestSheet extends StatefulWidget {
  static const int noteMaxLength = 500;

  final InstrumentDto instrument;

  const RentalRequestSheet({super.key, required this.instrument});

  @override
  State<RentalRequestSheet> createState() => _RentalRequestSheetState();
}

class _RentalRequestSheetState extends State<RentalRequestSheet>
    with FormSubmitState<RentalRequestSheet> {
  final TextEditingController _note = TextEditingController();

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

    await submit(() async {
      final note = _note.text.trim();
      final rental = await rentals.createRequest(
        RentalCreateRequest(
          instrumentId: widget.instrument.id,
          note: note.isEmpty ? null : note,
        ),
      );
      if (!mounted) return;
      navigator.pop();
      navigator.pushNamed(
        AppRoutes.rentalDetail,
        arguments: RentalDetailArgs(rental.id),
      );
      messenger.showSnackBar(
        SnackBar(
          content: Text(rentalRequestSentMessage(rental.otherPendingRequests)),
        ),
      );
    });
  }

  @override
  Widget build(BuildContext context) {
    final error = submitError;
    return AppSheetBody(
      title: 'Zahtjev za iznajmljivanje',
      subtitle: _instrumentName,
      children: [
        AppTextField(
          controller: _note,
          label: 'Napomena',
          minLines: 3,
          maxLines: 5,
          maxLength: RentalRequestSheet.noteMaxLength,
          textInputAction: TextInputAction.newline,
        ),
        const SizedBox(height: AppSpacing.sm),
        SubmitButton(
          label: 'Pošalji zahtjev',
          onPressed: _submit,
          isBusy: isSubmitting,
        ),
        if (error != null) ...[
          const SizedBox(height: AppSpacing.md),
          ErrorBanner(message: error),
        ],
      ],
    );
  }
}
