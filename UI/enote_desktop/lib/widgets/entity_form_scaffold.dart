import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';
import 'entity_form_sections.dart';
import 'form_submit_state.dart';

enum EntityFormPresentation { page, dialog }

class EntityFormScaffold extends StatefulWidget {
  final String title;
  final List<Widget> Function(BuildContext context) fieldsBuilder;
  final Future<bool> Function() onSave;
  final bool isEditMode;
  final String saveLabel;
  final String savedMessage;
  final String? deletedMessage;
  final Future<bool> Function()? onDelete;

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
    this.deletedMessage,
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
        final deletedMessage = widget.deletedMessage;
        if (deletedMessage != null) {
          ScaffoldMessenger.of(context)
              .showSnackBar(SnackBar(content: Text(deletedMessage)));
        }
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

  Widget _deleteButton() {
    final button = EntityDeleteButton(
      isSaving: _isSaving,
      isDeleting: _isDeleting,
      onPressed: widget.onDelete == null ? null : _delete,
      label: widget.deleteLabel,
    );
    if (widget.onDelete == null && widget.deleteDisabledReason != null) {
      return Tooltip(message: widget.deleteDisabledReason!, child: button);
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
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: AppSize.formMaxWidth),
            child: ListView(
              padding: const EdgeInsets.all(AppSpacing.xl),
              children: _spacedFields(widget.fieldsBuilder(context)),
            ),
          ),
        ),
      ),
      bottomNavigationBar: _buildPageActions(context, showDelete),
    );
  }

  List<Widget> _spacedFields(List<Widget> fields) {
    final spaced = <Widget>[];
    for (var i = 0; i < fields.length; i++) {
      if (i > 0) spaced.add(const SizedBox(height: AppSpacing.lg));
      spaced.add(fields[i]);
    }
    return spaced;
  }

  Widget _buildPageActions(BuildContext context, bool showDelete) {
    final actionsBusy = _isSaving || _isDeleting;
    return Container(
      decoration: const BoxDecoration(
        color: AppTheme.background,
        border: Border(top: BorderSide(color: AppTheme.outline)),
      ),
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.xl,
        vertical: AppSpacing.md,
      ),
      child: Center(
        heightFactor: 1,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: AppSize.formMaxWidth),
          child: Row(
            children: [
              if (showDelete) _deleteButton(),
              const Spacer(),
              TextButton(
                onPressed: actionsBusy
                    ? null
                    : () => Navigator.of(context).pop(_didSave),
                child: const Text('Otkaži'),
              ),
              const SizedBox(width: AppSpacing.md),
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
                if (showDelete) ...[_deleteButton(), const Spacer()],
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
