import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/detail_row.dart';
import '../../../widgets/dialog_shell.dart';
import 'instrument_form_screen.dart';
import 'instrument_provider.dart';

class InstrumentDetailDialog extends StatefulWidget {
  final InstrumentDto instrument;

  const InstrumentDetailDialog({super.key, required this.instrument});

  static Future<bool?> show(BuildContext context, InstrumentDto instrument) {
    return showDialog<bool>(
      context: context,
      builder: (_) => InstrumentDetailDialog(instrument: instrument),
    );
  }

  @override
  State<InstrumentDetailDialog> createState() => _InstrumentDetailDialogState();
}

class _InstrumentDetailDialogState extends State<InstrumentDetailDialog> {
  late InstrumentDto _instrument;
  bool _isDeleting = false;

  @override
  void initState() {
    super.initState();
    _instrument = widget.instrument;
  }

  Future<void> _openEdit() async {
    final updated = await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => InstrumentFormScreen(
        existing: _instrument,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    if (updated == true && mounted) {
      Navigator.of(context).pop(true);
    }
  }

  Future<void> _delete() async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Potvrdite brisanje',
      message: 'Da li ste sigurni da želite da obrišete ovaj zapis?',
    );
    if (confirmed != true) return;
    if (!mounted) return;

    setState(() => _isDeleting = true);
    try {
      await context.read<InstrumentProvider>().remove(_instrument.id);
      if (mounted) {
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _isDeleting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final imagePath = _instrument.imagePath;
    final hasImage = imagePath != null && imagePath.isNotEmpty;

    return DialogShell(
      title: _instrument.model,
      width: DialogShellWidth.lg,
      onClose: () => Navigator.of(context).pop(false),
      body: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Flexible(
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.xl,
                AppSpacing.lg,
                AppSpacing.xl,
                AppSpacing.lg,
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (hasImage) ...[
                    _buildImage(context, imagePath),
                    const SizedBox(width: AppSpacing.xl),
                  ],
                  Expanded(child: _buildFacts()),
                ],
              ),
            ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.xl,
              AppSpacing.md,
              AppSpacing.xl,
              AppSpacing.lg,
            ),
            child: _buildActions(),
          ),
        ],
      ),
    );
  }

  Widget _buildImage(BuildContext context, String imagePath) {
    return SizedBox(
      width: AppSize.detailImageWidth,
      child: AspectRatio(
        aspectRatio: 1,
        child: ClipRRect(
          borderRadius: BorderRadius.circular(AppRadius.lg),
          child: networkImageOrPlaceholder(
            imagePath,
            context.read<ApiClient>(),
            size: double.infinity,
            borderRadius: AppRadius.lg,
            placeholder: () => Container(
              color: AppTheme.background,
              child: const Center(
                child: Icon(
                  Icons.music_note,
                  size: AppSize.stateIcon,
                  color: AppTheme.textTertiary,
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildFacts() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        DetailRow(
          icon: Icons.business_outlined,
          label: 'Proizvođač',
          value: _instrument.manufacturerName,
        ),
        DetailRow(
          icon: Icons.category_outlined,
          label: 'Tip instrumenta',
          value: _instrument.instrumentType,
        ),
        DetailRow(
          icon: Icons.payments_outlined,
          label: 'Mjesečna naknada',
          value: formatKM(_instrument.monthlyFee),
        ),
        DetailRow(
          icon: Icons.description_outlined,
          label: 'Opis',
          value: orDash(_instrument.description),
        ),
        DetailRow(
          icon: Icons.check_circle_outline,
          label: 'Dostupnost',
          value: _instrument.isAvailable ? 'Dostupan' : 'Nije dostupan',
        ),
      ],
    );
  }

  Widget _buildActions() {
    return Row(
      children: [
        Tooltip(
          message: _instrument.deleteBlockedReason ?? 'Obriši',
          child: OutlinedButton.icon(
            onPressed: _isDeleting || _instrument.deleteBlockedReason != null
                ? null
                : _delete,
            style: OutlinedButton.styleFrom(
              foregroundColor: AppTheme.error,
              side: const BorderSide(color: AppTheme.error),
            ),
            icon: _isDeleting
                ? const SizedBox(
                    width: AppSize.spinnerSm,
                    height: AppSize.spinnerSm,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: AppTheme.error,
                    ),
                  )
                : const Icon(Icons.delete_outline),
            label: const Text('Obriši'),
          ),
        ),
        const Spacer(),
        TextButton(
          onPressed: _isDeleting
              ? null
              : () => Navigator.of(context).pop(false),
          child: const Text('Zatvori'),
        ),
        const SizedBox(width: AppSpacing.md),
        OutlinedButton.icon(
          onPressed: _isDeleting ? null : _openEdit,
          icon: const Icon(Icons.edit_outlined),
          label: const Text('Uredi'),
        ),
      ],
    );
  }
}
