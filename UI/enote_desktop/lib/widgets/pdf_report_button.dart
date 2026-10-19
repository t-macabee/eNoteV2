import 'dart:io';
import 'dart:typed_data';

import 'package:enote_core/enote_core.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:printing/printing.dart';
import 'package:provider/provider.dart';

String reportFileName(String report) {
  final now = DateTime.now();
  final stamp = '${now.year}'
      '${now.month.toString().padLeft(2, '0')}'
      '${now.day.toString().padLeft(2, '0')}';
  return '$report-$stamp.pdf';
}

typedef PdfSaver = Future<String?> Function(Uint8List bytes, String fileName);

class PdfReportButton extends StatefulWidget {
  final String label;
  final String fileName;
  final String endpoint;

  final PdfSaver? savePdf;

  const PdfReportButton({
    super.key,
    required this.label,
    required this.fileName,
    required this.endpoint,
    this.savePdf,
  });

  @override
  State<PdfReportButton> createState() => _PdfReportButtonState();
}

class _PdfReportButtonState extends State<PdfReportButton> {
  bool _isLoading = false;

  Future<Uint8List?> _load() async {
    setState(() => _isLoading = true);
    try {
      final client = Provider.of<ApiClient>(context, listen: false);
      final response = await client.get(widget.endpoint);
      throwIfError(response);
      return response.bodyBytes;
    } catch (e) {
      _showError(e);
      return null;
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  static Future<String?> _save(Uint8List bytes, String fileName) async {
    final path = await FilePicker.platform.saveFile(
      dialogTitle: 'Sačuvaj izvještaj',
      fileName: fileName,
      type: FileType.custom,
      allowedExtensions: const ['pdf'],
    );
    if (path == null) return null;
    await File(path).writeAsBytes(bytes);
    return path;
  }

  Future<void> _print() async {
    try {
      final bytes = await _load();
      if (bytes == null) return;
      await Printing.layoutPdf(
        onLayout: (_) async => bytes,
        name: widget.fileName,
      );
    } catch (e) {
      _showError(e);
    }
  }

  Future<void> _download() async {
    final bytes = await _load();
    if (bytes == null) return;
    try {
      final path = await (widget.savePdf ?? _save)(bytes, widget.fileName);
      if (path == null || !mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Izvještaj sačuvan: $path')),
      );
    } catch (e) {
      _showError(e);
    }
  }

  void _showError(Object error) {
    if (!mounted) return;
    ErrorBanner.show(context, message: userMessage(error));
  }

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(widget.label),
        const SizedBox(width: 8),
        IconButton(
          icon: const Icon(Icons.print),
          tooltip: 'Ispis',
          onPressed: _isLoading ? null : _print,
        ),
        IconButton(
          icon: const Icon(Icons.download),
          tooltip: 'Preuzmi',
          onPressed: _isLoading ? null : _download,
        ),
        if (_isLoading)
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 8),
            child: SizedBox(
              width: 16,
              height: 16,
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
          ),
      ],
    );
  }
}
