import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

/// Generic add/edit form scaffold shared by every desktop CRUD screen.
///
/// - Renders an AppBar with an "X" close button (RS2 UX rule).
/// - Wraps the given fields in a [Form] and validates on save.
/// - On successful save of a *new* entity, clears the fields and stays open
///   (so the user can keep adding records); on successful save of an
///   *existing* entity, closes the screen and returns to the list.
class EntityFormScaffold extends StatefulWidget {
  final String title;
  final List<Widget> Function(BuildContext context) fieldsBuilder;
  final Future<bool> Function() onSave;
  final bool isEditMode;
  final String saveLabel;
  final String savedMessage;

  /// Called right after [FormState.reset] on a successful create, so a
  /// screen can null out any state it mirrors outside the [Form] (e.g. a
  /// date picker's `DateTime?` field) — [FormState.reset] only resets the
  /// widgets it owns, not a screen's own fields.
  final VoidCallback? onReset;

  const EntityFormScaffold({
    super.key,
    required this.title,
    required this.fieldsBuilder,
    required this.onSave,
    this.isEditMode = false,
    this.saveLabel = 'Sačuvaj',
    this.savedMessage = 'Uspješno sačuvano.',
    this.onReset,
  });

  @override
  State<EntityFormScaffold> createState() => _EntityFormScaffoldState();
}

class _EntityFormScaffoldState extends State<EntityFormScaffold> {
  final _formKey = GlobalKey<FormState>();
  bool _isSaving = false;

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    setState(() => _isSaving = true);
    try {
      final success = await widget.onSave();
      if (!mounted) return;

      if (success) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(widget.savedMessage)),
        );
        if (widget.isEditMode) {
          Navigator.of(context).pop(true);
        } else {
          _formKey.currentState?.reset();
          widget.onReset?.call();
        }
      }
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _isSaving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.title),
        leading: IconButton(
          icon: const Icon(Icons.close),
          tooltip: 'Zatvori',
          onPressed: () => Navigator.of(context).pop(false),
        ),
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            ...widget.fieldsBuilder(context),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: _isSaving ? null : _save,
              icon: _isSaving
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.save),
              label: Text(widget.saveLabel),
            ),
          ],
        ),
      ),
    );
  }
}
