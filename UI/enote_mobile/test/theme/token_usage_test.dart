import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

const _typeScaleFile = 'lib/theme/app_theme.dart';

final _fontSizeKey = RegExp(r'fontSize:');
final _rawColor = RegExp(r'Colors\.');
final _nonZeroNumber = RegExp(r'(?<![\w.])(?!0(?![\d.]))\d+(\.\d+)?');
final _sizedBoxCall = RegExp(r'SizedBox(?:\.square)?\(');
final _sizedBoxExtent = RegExp(r'\b(width|height|dimension):\s*(?!0\b)\d');
final _edgeInsetsCall = RegExp(r'EdgeInsets(?:Directional)?\.\w+\(');
final _radiusCall = RegExp(r'Radius\.circular\(');

List<File> _dartFiles() =>
    Directory('lib')
        .listSync(recursive: true)
        .whereType<File>()
        .where((file) => file.path.endsWith('.dart'))
        .toList();

String _path(File file) => file.path.replaceAll('\\', '/');

List<String> _lineHits(RegExp pattern, {String? exceptFile}) {
  final hits = <String>[];
  for (final file in _dartFiles()) {
    final path = _path(file);
    if (exceptFile != null && path.endsWith(exceptFile)) continue;
    final lines = file.readAsLinesSync();
    for (var i = 0; i < lines.length; i++) {
      if (pattern.hasMatch(lines[i])) {
        hits.add('$path:${i + 1}: ${lines[i].trim()}');
      }
    }
  }
  return hits;
}

String _argumentsAfter(String source, Match call) {
  final open = call.end - 1;
  var depth = 0;
  for (var i = open; i < source.length; i++) {
    if (source[i] == '(') depth++;
    if (source[i] == ')' && --depth == 0) {
      return source.substring(open + 1, i);
    }
  }
  return '';
}

List<String> _callHits(
  RegExp call,
  bool Function(String arguments) hasLiteral,
) {
  final hits = <String>[];
  for (final file in _dartFiles()) {
    final path = _path(file);
    final source = file.readAsStringSync();
    for (final match in call.allMatches(source)) {
      final arguments = _argumentsAfter(source, match);
      if (hasLiteral(arguments)) {
        final line = '\n'.allMatches(source.substring(0, match.start)).length;
        hits.add('$path:${line + 1}: ${match.group(0)}$arguments)');
      }
    }
  }
  return hits;
}

void main() {
  test('mobile lib has no fontSize literal', () {
    final hits = _lineHits(_fontSizeKey, exceptFile: _typeScaleFile);
    expect(hits, isEmpty, reason: hits.join('\n'));
  });

  test('mobile lib has no raw Colors', () {
    final hits = _lineHits(_rawColor);
    expect(hits, isEmpty, reason: hits.join('\n'));
  });

  test('mobile lib has no numeric EdgeInsets', () {
    final hits = _callHits(
      _edgeInsetsCall,
      (arguments) => _nonZeroNumber.hasMatch(arguments),
    );
    expect(hits, isEmpty, reason: hits.join('\n'));
  });

  test('mobile lib has no numeric SizedBox extent', () {
    final hits = _callHits(
      _sizedBoxCall,
      (arguments) => _sizedBoxExtent.hasMatch(arguments),
    );
    expect(hits, isEmpty, reason: hits.join('\n'));
  });

  test('mobile lib has no numeric Radius', () {
    final hits = _callHits(
      _radiusCall,
      (arguments) => _nonZeroNumber.hasMatch(arguments),
    );
    expect(hits, isEmpty, reason: hits.join('\n'));
  });
}
