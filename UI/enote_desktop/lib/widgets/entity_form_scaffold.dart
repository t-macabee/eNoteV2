import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

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
  final VoidCallback? onReset;
  final bool showCloseButton;
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

  static Future<bool?> showAsDialog(
    BuildContext context, {
    required WidgetBuilder builder,
  }) {
    return showDialog<bool>(context: context, builder: builder);
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
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(widget.savedMessage)));
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
