import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:printing/printing.dart';

import 'package:enote_core/enote_core.dart';

class PdfReportButton extends StatefulWidget {
  final String label;
  final String fileName;
  final Future<Uint8List> Function() fetchPdf;

  const PdfReportButton({
    super.key,
    required this.label,
    required this.fileName,
    required this.fetchPdf,
  });

  @override
  State<PdfReportButton> createState() => _PdfReportButtonState();
}

class _PdfReportButtonState extends State<PdfReportButton> {
  bool _isLoading = false;

  Future<Uint8List?> _load() async {
    setState(() => _isLoading = true);
    try {
      return await widget.fetchPdf();
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
      return null;
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _print() async {
    final bytes = await _load();
    if (bytes == null) return;
    await Printing.layoutPdf(
      onLayout: (_) async => bytes,
      name: widget.fileName,
    );
  }

  Future<void> _download() async {
    final bytes = await _load();
    if (bytes == null) return;
    await Printing.sharePdf(bytes: bytes, filename: widget.fileName);
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
