import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/detail_row.dart';
import 'instrument_form_screen.dart';
import 'instrument_provider.dart';

class InstrumentDetailDialog extends StatefulWidget {
  final InstrumentDto instrument;

  const InstrumentDetailDialog({
    super.key,
    required this.instrument,
  });

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
    final maxHeight = MediaQuery.sizeOf(context).height * 0.8;
    final apiClient = context.read<ApiClient>();

    return AlertDialog(
      constraints: BoxConstraints(maxWidth: 640, maxHeight: maxHeight),
      titlePadding: const EdgeInsets.fromLTRB(24, 20, 8, 0),
      title: Row(
        children: [
          Expanded(
            child: Text(
              _instrument.model,
              overflow: TextOverflow.ellipsis,
            ),
          ),
          IconButton(
            icon: const Icon(Icons.close),
            tooltip: 'Zatvori',
            onPressed: () => Navigator.of(context).pop(false),
          ),
        ],
      ),
      contentPadding: const EdgeInsets.fromLTRB(24, 20, 24, 0),
      content: SizedBox(
        width: 520,
        child: SingleChildScrollView(
          padding: const EdgeInsets.only(bottom: 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (_instrument.imagePath != null &&
                  _instrument.imagePath!.isNotEmpty) ...[
                AspectRatio(
                  aspectRatio: 1.5,
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(12),
                    child: networkImageOrPlaceholder(
                      _instrument.imagePath,
                      apiClient,
                      size: double.infinity,
                      borderRadius: 12,
                      placeholder: () => Container(
                        color: AppTheme.background,
                        child: const Center(
                          child: Icon(
                            Icons.music_note,
                            size: 48,
                            color: AppTheme.textTertiary,
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 16),
              ],
              DetailRow(
                icon: Icons.music_note_outlined,
                label: 'Model',
                value: _instrument.model,
              ),
              DetailRow(
                icon: Icons.business_outlined,
                label: 'Proizvođač',
                value: _instrument.manufacturer,
              ),
              DetailRow(
                icon: Icons.category_outlined,
                label: 'Tip instrumenta',
                value: _instrument.instrumentType,
              ),
              DetailRow(
                icon: Icons.description_outlined,
                label: 'Opis',
                value: (_instrument.description != null &&
                        _instrument.description!.isNotEmpty)
                    ? _instrument.description!
                    : '-',
              ),
              DetailRow(
                icon: Icons.check_circle_outline,
                label: 'Dostupnost',
                value: _instrument.isAvailable ? 'Dostupan' : 'Nije dostupan',
              ),
            ],
          ),
        ),
      ),
      actionsPadding: const EdgeInsets.fromLTRB(24, 0, 24, 16),
      actions: [
        Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Divider(),
            const SizedBox(height: 12),
            Row(
              children: [
                OutlinedButton.icon(
                  onPressed: _isDeleting ? null : _delete,
                  style: OutlinedButton.styleFrom(
                    foregroundColor: AppTheme.error,
                    side: const BorderSide(color: AppTheme.error),
                  ),
                  icon: _isDeleting
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: AppTheme.error,
                          ),
                        )
                      : const Icon(Icons.delete_outline),
                  label: const Text('Obriši'),
                ),
                const Spacer(),
                TextButton(
                  onPressed:
                      _isDeleting ? null : () => Navigator.of(context).pop(false),
                  child: const Text('Zatvori'),
                ),
                const SizedBox(width: 12),
                OutlinedButton.icon(
                  onPressed: _isDeleting ? null : _openEdit,
                  icon: const Icon(Icons.edit_outlined),
                  label: const Text('Uredi'),
                ),
              ],
            ),
          ],
        ),
      ],
    );
  }
}
