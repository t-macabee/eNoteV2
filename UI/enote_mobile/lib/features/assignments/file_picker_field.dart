import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';

class PickedAssignmentFile {
  final Uint8List bytes;
  final String fileName;
  final String contentType;

  PickedAssignmentFile({
    required this.bytes,
    required this.fileName,
    required this.contentType,
  });
}

const _allowedAssignmentExtensions = ['pdf', 'jpg', 'jpeg', 'png'];

String? validateAssignmentFile(String fileName, int byteLength) {
  if (!_allowedAssignmentExtensions.contains(fileExtension(fileName))) {
    return 'Dozvoljeni formati: PDF, JPG, PNG.';
  }
  if (byteLength > maxUploadBytes) {
    return 'Datoteka je veća od 5 MB.';
  }
  return null;
}

String assignmentContentType(String fileName) {
  return switch (fileExtension(fileName)) {
    'pdf' => 'application/pdf',
    'jpg' || 'jpeg' => 'image/jpeg',
    'png' => 'image/png',
    _ => 'application/octet-stream',
  };
}

String formatFileSize(int bytes) {
  if (bytes < 1024) return '$bytes B';
  if (bytes < 1024 * 1024) {
    return '${(bytes / 1024).toStringAsFixed(1)} KB';
  }
  return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
}

bool isAssignmentImage(String? filePath) {
  if (filePath == null) return false;
  return const ['jpg', 'jpeg', 'png'].contains(fileExtension(filePath));
}

String assignmentFileLabel(String? filePath) {
  if (filePath == null || filePath.isEmpty) return 'Nema datoteke';
  return isAssignmentImage(filePath) ? 'Slika' : 'PDF datoteka';
}

class FilePickerField extends StatefulWidget {
  final bool enabled;
  final ValueChanged<PickedAssignmentFile?> onChanged;
  final Future<PickedAssignmentFile?> Function()? pickOverride;

  const FilePickerField({
    super.key,
    this.enabled = true,
    required this.onChanged,
    this.pickOverride,
  });

  @override
  State<FilePickerField> createState() => _FilePickerFieldState();
}

class _FilePickerFieldState extends State<FilePickerField> {
  PickedAssignmentFile? _picked;
  String? _error;

  Future<void> _pick() async {
    if (!widget.enabled) return;
    final PickedAssignmentFile? candidate;
    if (widget.pickOverride != null) {
      candidate = await widget.pickOverride!();
      if (candidate == null || !mounted) return;
    } else {
      final file = await FilePicker.pickFile(type: FileType.any);
      if (file == null || !mounted) return;
      final Uint8List bytes;
      try {
        bytes = await file.readAsBytes();
      } on Exception {
        if (!mounted) return;
        setState(
          () => _error = 'Datoteku nije moguće pročitati. Pokušajte ponovo.',
        );
        return;
      }
      candidate = PickedAssignmentFile(
        bytes: bytes,
        fileName: file.name,
        contentType: assignmentContentType(file.name),
      );
    }
    final message = validateAssignmentFile(
      candidate.fileName,
      candidate.bytes.length,
    );
    setState(() {
      _error = message;
      _picked = message == null ? candidate : null;
    });
    widget.onChanged(message == null ? candidate : null);
  }

  void _clear() {
    setState(() {
      _picked = null;
      _error = null;
    });
    widget.onChanged(null);
  }

  @override
  Widget build(BuildContext context) {
    final hintStyle = Theme.of(context).textTheme.bodySmall
        ?.copyWith(color: AppTheme.textSecondary);
    final picked = _picked;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        OutlinedButton.icon(
          onPressed: widget.enabled ? _pick : null,
          icon: const Icon(Icons.attach_file_outlined),
          label: const Text('Odaberi datoteku'),
        ),
        const SizedBox(height: AppSpacing.xs),
        Text(
          'PDF, JPG ili PNG · do 5 MB',
          textAlign: TextAlign.center,
          style: hintStyle,
        ),
        if (picked != null) ...[
          const SizedBox(height: AppSpacing.sm),
          Row(
            children: [
              const Icon(Icons.description_outlined),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  '${picked.fileName} · ${formatFileSize(picked.bytes.length)}',
                ),
              ),
              IconButton(
                icon: const Icon(Icons.close),
                tooltip: 'Ukloni',
                onPressed: widget.enabled ? _clear : null,
              ),
            ],
          ),
        ],
        if (_error != null) ...[
          const SizedBox(height: AppSpacing.xs),
          Text(
            _error!,
            style: Theme.of(context).textTheme.bodySmall
                ?.copyWith(color: AppTheme.error),
          ),
        ],
      ],
    );
  }
}
