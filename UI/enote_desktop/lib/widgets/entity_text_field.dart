import 'package:enote_core/enote_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

class EntityTextField extends StatelessWidget {
  final TextEditingController controller;
  final String label;
  final bool required;
  final String? Function(String?)? validator;
  final String? hintText;
  final int? maxLines;
  final int? minLines;
  final TextInputType? keyboardType;
  final List<TextInputFormatter>? inputFormatters;
  final bool enabled;

  const EntityTextField({
    super.key,
    required this.controller,
    required this.label,
    this.required = true,
    this.validator,
    this.hintText,
    this.maxLines = 1,
    this.minLines,
    this.keyboardType,
    this.inputFormatters,
    this.enabled = true,
  });

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      decoration: InputDecoration(labelText: label, hintText: hintText),
      validator: validator ?? (required ? Validators.required(label) : null),
      maxLines: maxLines,
      minLines: minLines,
      keyboardType: keyboardType,
      inputFormatters: inputFormatters,
      enabled: enabled,
    );
  }
}
