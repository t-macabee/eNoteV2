import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

/// How an [EntityFormScaffold] is presented to the user.
enum EntityFormPresentation {
  /// Full page — [Scaffold] with an [AppBar] (the default, so existing call
  /// sites keep their current behavior).
  page,

  /// Bounded, scrollable [AlertDialog] with an "X" close affordance (RS2 UX
  /// rule) in the dialog's title row instead of an AppBar leading icon.
  dialog,
}

/// Generic add/edit form scaffold shared by every desktop CRUD screen.
///
/// - Page mode ([EntityFormPresentation.page], the default) renders an
///   AppBar with an "X" close button (RS2 UX rule), unless
///   [EntityFormScaffold.showCloseButton] is `false`. Screens that are not
///   pushed onto the navigation stack (e.g. opened straight from the
///   drawer) have no route to pop, so they pass `false`.
/// - Dialog mode ([EntityFormPresentation.dialog]) renders the same Form,
///   field list and save button inside a bounded, scrollable [AlertDialog];
///   the "X" appears in the dialog's title row. Open it with
///   [EntityFormScaffold.showAsDialog].
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

  /// Whether to render the AppBar's close "X" (page mode only — dialog mode
  /// always shows its dialog-appropriate "X").
  final bool showCloseButton;

  /// Whether this form is shown as a full page or as a bounded dialog.
  final EntityFormPresentation presentation;

  const EntityFormScaffold({
    super.key,
    required this.title,
    required this.fieldsBuilder,
    required this.onSave,
    this.isEditMode = false,
    this.saveLabel = 'Sačuvaj',
    this.savedMessage = 'Uspješno sačuvano.',
    this.onReset,
    this.showCloseButton = true,
    this.presentation = EntityFormPresentation.page,
  });

  /// Shows [builder]'s widget — i.e. an [EntityFormScaffold] in dialog
  /// presentation mode — as a bounded form dialog instead of a pushed page.
  ///
  /// This is the dialog counterpart of pushing a
  /// [MaterialPageRoute]: it hands the resulting route to the program,
  /// mirroring the sizing constraints configured by the scaffold itself.
  /// Resolves to `true` when an edit was saved, `false` when closed via the
  /// "X", and `null` when dismissed by tapping the barrier.
  static Future<bool?> showAsDialog(
    BuildContext context, {
    required WidgetBuilder builder,
  }) {
    return showDialog<bool>(
      context: context,
      builder: builder,
    );
  }

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
    return switch (widget.presentation) {
      EntityFormPresentation.page => _buildPage(context),
      EntityFormPresentation.dialog => _buildDialog(context),
    };
  }

  Widget _buildSaveButton() {
    return FilledButton.icon(
      onPressed: _isSaving ? null : _save,
      icon: _isSaving
          ? const SizedBox(
              width: 16,
              height: 16,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : const Icon(Icons.save),
      label: Text(widget.saveLabel),
    );
  }

  Widget _buildPage(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.title),
        leading: widget.showCloseButton
            ? IconButton(
                icon: const Icon(Icons.close),
                tooltip: 'Zatvori',
                onPressed: () => Navigator.of(context).pop(false),
              )
            : null,
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            ...widget.fieldsBuilder(context),
            const SizedBox(height: 24),
            _buildSaveButton(),
          ],
        ),
      ),
    );
  }

  Widget _buildDialog(BuildContext context) {
    final maxHeight = MediaQuery.sizeOf(context).height * 0.8;
    return AlertDialog(
      constraints: BoxConstraints(maxWidth: 640, maxHeight: maxHeight),
      titlePadding: const EdgeInsets.fromLTRB(24, 8, 4, 0),
      title: Row(
        children: [
          Expanded(child: Text(widget.title)),
          IconButton(
            icon: const Icon(Icons.close),
            tooltip: 'Zatvori',
            onPressed: () => Navigator.of(context).pop(false),
          ),
        ],
      ),
      contentPadding: const EdgeInsets.fromLTRB(24, 8, 24, 0),
      content: SizedBox(
        width: 520,
        child: SingleChildScrollView(
          padding: const EdgeInsets.only(bottom: 24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                ...widget.fieldsBuilder(context),
                const SizedBox(height: 24),
                _buildSaveButton(),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
