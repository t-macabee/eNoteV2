import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import '../api/api_client.dart';
import 'network_image.dart';

typedef ImageUploadCallback = Future<String?> Function(Uint8List bytes, String fileName, String contentType);

class ImageField extends StatefulWidget {
  final String? imageUrl;
  final double size;
  final double borderRadius;
  final bool editable;
  final ImageUploadCallback? onUpload;
  final Future<Uint8List?> Function()? imagePicker;

  final ApiClient? apiClient;
  final AlignmentGeometry alignment;

  const ImageField({
    super.key,
    this.imageUrl,
    this.size = 120,
    this.borderRadius = 8,
    this.editable = true,
    this.onUpload,
    this.imagePicker,
    this.apiClient,
    this.alignment = Alignment.centerLeft,
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
    return Align(
      alignment: widget.alignment,
      child: GestureDetector(
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

    return networkImageOrPlaceholder(
      widget.imageUrl,
      widget.apiClient,
      size: widget.size,
      borderRadius: widget.borderRadius,
      placeholder: _placeholder,
    );
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
      await widget.onUpload!(bytes, _localFileName!, _localContentType!);
      setState(() => _isUploading = false);
    }
  }
}
