import 'package:flutter/material.dart';

class AppTextField extends StatefulWidget {
  final TextEditingController controller;
  final String label;
  final String? Function(String?)? validator;
  final TextInputAction textInputAction;
  final TextInputType? keyboardType;
  final bool obscure;
  final bool readOnly;
  final bool autofocus;
  final String? errorText;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onFieldSubmitted;
  final int? maxLength;
  final int? minLines;
  final int? maxLines;

  const AppTextField({
    super.key,
    required this.controller,
    required this.label,
    this.validator,
    this.textInputAction = TextInputAction.next,
    this.keyboardType,
    this.obscure = false,
    this.readOnly = false,
    this.autofocus = false,
    this.errorText,
    this.onChanged,
    this.onFieldSubmitted,
    this.maxLength,
    this.minLines,
    this.maxLines = 1,
  });

  @override
  State<AppTextField> createState() => _AppTextFieldState();
}

class _AppTextFieldState extends State<AppTextField> {
  bool _obscured = true;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: widget.controller,
      decoration: InputDecoration(
        labelText: widget.label,
        errorText: widget.errorText,
        suffixIcon: widget.obscure
            ? IconButton(
                tooltip: _obscured ? 'Prikaži lozinku' : 'Sakrij lozinku',
                icon: Icon(_obscured ? Icons.visibility : Icons.visibility_off),
                onPressed: () => setState(() => _obscured = !_obscured),
              )
            : null,
      ),
      obscureText: widget.obscure && _obscured,
      autofocus: widget.autofocus,
      keyboardType: widget.keyboardType,
      textInputAction: widget.textInputAction,
      readOnly: widget.readOnly,
      onChanged: widget.onChanged,
      onFieldSubmitted: widget.onFieldSubmitted,
      validator: widget.validator,
      maxLength: widget.maxLength,
      minLines: widget.minLines,
      maxLines: widget.maxLines,
    );
  }
}
