import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';
import 'entity_form_sections.dart';
import 'form_submit_state.dart';

/// How an [EntityFormScaffold] is presented to the user.
enum EntityFormPresentation {
  /// Full page — [Scaffold] with an [AppBar] (the default).
  page,

  /// Bounded, scrollable [AlertDialog]. Open with
  /// [EntityFormScaffold.showAsDialog].
  dialog,
}

class EntityFormScaffold extends StatefulWidget {
  final String title;
  final List<Widget> Function(BuildContext context) fieldsBuilder;
  final Future<bool> Function() onSave;
  final bool isEditMode;
  final String saveLabel;
  final String savedMessage;
  final Future<bool> Function()? onDelete;

  /// When set and [onDelete] is null, the delete button is shown disabled with
  /// this text as its tooltip.
  final String? deleteDisabledReason;
  final String deleteLabel;
  final String deleteConfirmTitle;
  final String deleteConfirmMessage;
  final VoidCallback? onReset;
  final bool showCloseButton;
  final EntityFormPresentation presentation;
  final bool closeAfterAdd;
  final bool saveEnabled;

  const EntityFormScaffold({
    super.key,
    required this.title,
    required this.fieldsBuilder,
    required this.onSave,
    this.isEditMode = false,
    this.saveLabel = 'Sačuvaj',
    this.savedMessage = 'Uspješno sačuvano.',
    this.onDelete,
    this.deleteDisabledReason,
    this.deleteLabel = 'Obriši',
    this.deleteConfirmTitle = 'Potvrdite brisanje',
    this.deleteConfirmMessage =
        'Da li ste sigurni da želite da obrišete ovaj zapis?',
    this.onReset,
    this.showCloseButton = true,
    this.presentation = EntityFormPresentation.page,
    this.closeAfterAdd = false,
    this.saveEnabled = true,
  });

  static Future<bool?> showAsDialog(
    BuildContext context, {
    required WidgetBuilder builder,
  }) {
    return showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: builder,
    );
  }

  @override
  State<EntityFormScaffold> createState() => _EntityFormScaffoldState();
}

class _EntityFormScaffoldState extends State<EntityFormScaffold>
    with FormSubmitState<EntityFormScaffold> {
  final _formKey = GlobalKey<FormState>();
  bool _isSaving = false;
  bool _isDeleting = false;
  bool _didSave = false;

  Future<void> _delete() async {
    final confirmed = await confirmDialog(
      context: context,
      title: widget.deleteConfirmTitle,
      message: widget.deleteConfirmMessage,
    );
    if (confirmed != true) return;
    if (!mounted) return;

    await submitWith((busy) => _isDeleting = busy, () async {
      final success = await widget.onDelete!();
      if (!mounted) return;
      if (success) {
        Navigator.of(context).pop(true);
      }
    });
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    await submitWith((busy) => _isSaving = busy, () async {
      final success = await widget.onSave();
      if (!mounted) return;

      if (success) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(widget.savedMessage)));
        if (widget.isEditMode) {
          Navigator.of(context).pop(true);
        } else if (widget.closeAfterAdd) {
          Navigator.of(context).pop(true);
        } else {
          _didSave = true;
          _formKey.currentState?.reset();
          widget.onReset?.call();
        }
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return switch (widget.presentation) {
      EntityFormPresentation.page => _buildPage(context),
      EntityFormPresentation.dialog => _buildDialog(context),
    };
  }

  /// Interleaves [spacing] gaps between fields for the page-mode [ListView],
  /// which has no `spacing` property unlike [Column].
  List<Widget> _withSpacing(List<Widget> fields, double spacing) {
    if (fields.length <= 1) return fields;
    final spaced = <Widget>[];
    for (var i = 0; i < fields.length; i++) {
      spaced.add(fields[i]);
      if (i < fields.length - 1) spaced.add(SizedBox(height: spacing));
    }
    return spaced;
  }

  Widget _deleteButton() {
    final button = EntityDeleteButton(
      isSaving: _isSaving,
      isDeleting: _isDeleting,
      onPressed: widget.onDelete == null ? null : _delete,
      label: widget.deleteLabel,
    );
    if (widget.onDelete == null && widget.deleteDisabledReason != null) {
      return Tooltip(
        message: widget.deleteDisabledReason!,
        child: button,
      );
    }
    return button;
  }

  Widget _buildPage(BuildContext context) {
    final showDelete =
        widget.isEditMode &&
        (widget.onDelete != null || widget.deleteDisabledReason != null);

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.title),
        leading: widget.showCloseButton
            ? IconButton(
                icon: const Icon(Icons.close),
                tooltip: 'Zatvori',
                onPressed: () => Navigator.of(context).pop(_didSave),
              )
            : null,
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            ..._withSpacing(widget.fieldsBuilder(context), 16),
            const SizedBox(height: 24),
            if (showDelete)
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  _deleteButton(),
                  EntitySaveButton(
                    isSaving: _isSaving,
                    isDeleting: _isDeleting,
                    enabled: widget.saveEnabled,
                    onPressed: _save,
                    label: widget.saveLabel,
                  ),
                ],
              )
            else
              EntitySaveButton(
                isSaving: _isSaving,
                isDeleting: _isDeleting,
                enabled: widget.saveEnabled,
                onPressed: _save,
                label: widget.saveLabel,
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildDialog(BuildContext context) {
    final maxHeight = MediaQuery.sizeOf(context).height * 0.8;
    final showDelete =
        widget.isEditMode &&
        (widget.onDelete != null || widget.deleteDisabledReason != null);

    return AlertDialog(
      constraints: BoxConstraints(maxWidth: 640, maxHeight: maxHeight),
      titlePadding: const EdgeInsets.fromLTRB(24, 20, 8, 0),
      title: Row(
        children: [
          Expanded(child: Text(widget.title)),
          IconButton(
            icon: const Icon(Icons.close),
            tooltip: 'Zatvori',
            onPressed: () => Navigator.of(context).pop(_didSave),
          ),
        ],
      ),
      contentPadding: const EdgeInsets.fromLTRB(24, 20, 24, 0),
      content: SizedBox(
        width: 520,
        child: SingleChildScrollView(
          padding: const EdgeInsets.only(bottom: 12),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 16,
              children: widget.fieldsBuilder(context),
            ),
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
                if (showDelete) ...[
                  _deleteButton(),
                  const Spacer(),
                ],
                if (!showDelete) const Spacer(),
                TextButton(
                  onPressed: (_isSaving || _isDeleting)
                      ? null
                      : () => Navigator.of(context).pop(_didSave),
                  child: const Text('Otkaži'),
                ),
                const SizedBox(width: 12),
                EntitySaveButton(
                  isSaving: _isSaving,
                  isDeleting: _isDeleting,
                  enabled: widget.saveEnabled,
                  onPressed: _save,
                  label: widget.saveLabel,
                ),
              ],
            ),
          ],
        ),
      ],
    );
  }
}

extension CrudProviderSave<T> on CrudProvider<T> {
  Future<T?> save({int? id, required Map<String, dynamic> request}) =>
      id == null ? insert(request) : update(id, request);
}
