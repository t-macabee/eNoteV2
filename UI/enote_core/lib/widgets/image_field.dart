import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

typedef ImageUploadCallback = Future<String?> Function(Uint8List bytes, String fileName, String contentType);

class ImageField extends StatefulWidget {
  final String? imageUrl;
  final String? placeholderAsset;
  final double size;
  final double borderRadius;
  final bool editable;
  final VoidCallback? onPick;
  final ImageUploadCallback? onUpload;
  final Future<Uint8List?> Function()? imagePicker;

  const ImageField({
    super.key,
    this.imageUrl,
    this.placeholderAsset,
    this.size = 120,
    this.borderRadius = 8,
    this.editable = true,
    this.onPick,
    this.onUpload,
    this.imagePicker,
  });

  @override
  State<ImageField> createState() => _ImageFieldState();
}

class _ImageFieldState extends State<ImageField> {
  Uint8List? _localBytes;
  String? _localFileName;
  String? _localContentType;
  bool _isUploading = false;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: widget.editable ? _pickImage : null,
      child: Container(
        width: widget.size,
        height: widget.size,
        decoration: BoxDecoration(
          color: Colors.grey.shade300,
          borderRadius: BorderRadius.circular(widget.borderRadius),
          border: widget.editable
              ? Border.all(color: Colors.grey.shade400, width: 2)
              : null,
        ),
        child: _buildContent(),
      ),
    );
  }

  Widget _buildContent() {
    if (_isUploading) {
      return const Center(child: CircularProgressIndicator(strokeWidth: 2));
    }

    if (_localBytes != null) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(widget.borderRadius),
        child: Image.memory(
          _localBytes!,
          width: widget.size,
          height: widget.size,
          fit: BoxFit.cover,
        ),
      );
    }

    if (widget.imageUrl != null && widget.imageUrl!.isNotEmpty) {
      final trimmed = widget.imageUrl!.trim();
      if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
        return ClipRRect(
          borderRadius: BorderRadius.circular(widget.borderRadius),
          child: Image.network(
            trimmed,
            width: widget.size,
            height: widget.size,
            fit: BoxFit.cover,
            errorBuilder: (_, _, _) => _placeholder(),
          ),
        );
      }
      if (trimmed.startsWith('/')) {
        return ClipRRect(
          borderRadius: BorderRadius.circular(widget.borderRadius),
          child: Image.file(
            File(trimmed),
            width: widget.size,
            height: widget.size,
            fit: BoxFit.cover,
            errorBuilder: (_, _, _) => _placeholder(),
          ),
        );
      }
    }

    if (widget.placeholderAsset != null) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(widget.borderRadius),
        child: Image.asset(
          widget.placeholderAsset!,
          width: widget.size,
          height: widget.size,
          fit: BoxFit.cover,
        ),
      );
    }

    return _placeholder();
  }

  Widget _placeholder() {
    return Icon(
      Icons.image,
      size: widget.size * 0.4,
      color: Colors.grey.shade600,
    );
  }

  Future<void> _pickImage() async {
    final picker = widget.imagePicker;
    if (picker == null) {
      widget.onPick?.call();
      return;
    }

    final bytes = await picker();
    if (bytes == null || !mounted) return;

    setState(() {
      _localBytes = bytes;
      _localFileName = 'image_${DateTime.now().millisecondsSinceEpoch}.jpg';
      _localContentType = 'image/jpeg';
    });

    if (widget.onUpload != null) {
      setState(() => _isUploading = true);
      final uploadedUrl = await widget.onUpload!(bytes, _localFileName!, _localContentType!);
      if (mounted && uploadedUrl != null) {
        widget.onPick?.call();
      }
      setState(() => _isUploading = false);
    }
  }
}
