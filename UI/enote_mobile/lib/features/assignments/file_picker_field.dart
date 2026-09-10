import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

/// A file picked for an assignment submission (S23).
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
const _assignmentSizeLimit = 5 * 1024 * 1024;

/// Pure validation shared by the widget and its tests: returns the message to
/// render **below** the picker, or null when the file is acceptable
/// (02 §11 `assignment.tooLarge` / `assignment.badType`, 03 R7).
String? validateAssignmentFile(String fileName, int byteLength) {
  final dot = fileName.lastIndexOf('.');
  final extension = dot == -1
      ? ''
      : fileName.substring(dot + 1).toLowerCase();
  if (!_allowedAssignmentExtensions.contains(extension)) {
    return 'Dozvoljeni formati: PDF, JPG, PNG.';
  }
  if (byteLength > _assignmentSizeLimit) {
    return 'Datoteka je veća od 5 MB.';
  }
  return null;
}

/// Content-type from the extension (`file_picker` does not report one).
String assignmentContentType(String fileName) {
  final dot = fileName.lastIndexOf('.');
  final extension = dot == -1
      ? ''
      : fileName.substring(dot + 1).toLowerCase();
  return switch (extension) {
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

/// Last segment of a stored `uploads/assignments/…` path (02 §4.7) — the
/// only part of [filePath] ever rendered.
String assignmentFileName(String? filePath) {
  if (filePath == null || filePath.isEmpty) return '—';
  final segments = filePath.split('/');
  return segments.isEmpty ? '—' : segments.last;
}

/// File picker for the assignment submission section (S23 state A, 02 §4.7).
///
/// The picker accepts any file and validates client-side (extension + 5 MB),
/// so rejected files surface the design messages below the control instead
/// of vanishing in an OS filter. [pickOverride] is the widget-test seam.
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
      final result = await FilePicker.platform.pickFiles(
        type: FileType.any,
        withData: true,
      );
      if (result == null || result.files.isEmpty || !mounted) return;
      final file = result.files.single;
      final bytes = file.bytes;
      if (bytes == null) return;
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
    final errorColor = Theme.of(context).colorScheme.error;
    final secondaryColor = Theme.of(context).textTheme.bodySmall?.color;
    final picked = _picked;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        OutlinedButton.icon(
          onPressed: widget.enabled ? _pick : null,
          icon: const Icon(Icons.attach_file_outlined),
          label: const Text('Odaberi datoteku'),
        ),
        const SizedBox(height: 4),
        Text(
          'PDF, JPG ili PNG · do 5 MB',
          textAlign: TextAlign.center,
          style: TextStyle(fontSize: 12, color: secondaryColor),
        ),
        if (picked != null) ...[
          const SizedBox(height: 8),
          Row(
            children: [
              const Icon(Icons.description_outlined),
              const SizedBox(width: 8),
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
          const SizedBox(height: 4),
          Text(
            _error!,
            style: TextStyle(fontSize: 12, color: errorColor),
          ),
        ],
      ],
    );
  }
}
