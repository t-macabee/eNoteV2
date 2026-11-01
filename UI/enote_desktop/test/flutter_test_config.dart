import 'dart:async';

import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

Future<void> testExecutable(FutureOr<void> Function() testMain) async {
  TestWidgetsFlutterBinding.ensureInitialized();

  final loader = FontLoader('Roboto')
    ..addFont(rootBundle.load('fonts/Roboto-Regular.ttf'))
    ..addFont(rootBundle.load('fonts/Roboto-Medium.ttf'))
    ..addFont(rootBundle.load('fonts/Roboto-SemiBold.ttf'))
    ..addFont(rootBundle.load('fonts/Roboto-Bold.ttf'));
  await loader.load();

  await testMain();
}
