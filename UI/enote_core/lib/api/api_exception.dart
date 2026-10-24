class ApiException implements Exception {
  final String message;
  final String code;
  final Map<String, List<String>> errors;

  ApiException(
    this.message, {
    this.code = '',
    this.errors = const {},
  });

  String? fieldError(String field) {
    final name = field.toLowerCase();
    for (final entry in errors.entries) {
      if (entry.key.toLowerCase() == name && entry.value.isNotEmpty) {
        return entry.value.first;
      }
    }
    return null;
  }

  @override
  String toString() => message;
}
