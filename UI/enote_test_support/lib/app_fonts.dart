import 'package:flutter/services.dart';

Future<void> loadAppFonts() async {
  final loader = FontLoader('packages/enote_core/Roboto')
    ..addFont(rootBundle.load('packages/enote_core/fonts/Roboto-Regular.ttf'))
    ..addFont(rootBundle.load('packages/enote_core/fonts/Roboto-Medium.ttf'))
    ..addFont(rootBundle.load('packages/enote_core/fonts/Roboto-SemiBold.ttf'))
    ..addFont(rootBundle.load('packages/enote_core/fonts/Roboto-Bold.ttf'));
  await loader.load();
}
