import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:enote_test_support/enote_test_support.dart';

Future<void> testExecutable(FutureOr<void> Function() testMain) async {
  TestWidgetsFlutterBinding.ensureInitialized();

  await loadAppFonts();

  await testMain();
}
