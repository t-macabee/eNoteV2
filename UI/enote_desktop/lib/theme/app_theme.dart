import 'package:flutter/material.dart';

import 'package:enote_core/theme/app_theme.dart';

export 'package:enote_core/theme/app_theme.dart';

final ThemeData desktopTheme = AppTheme.dark.copyWith(
  textTheme: AppTheme.dark.textTheme.apply(fontFamily: 'Roboto'),
  primaryTextTheme: AppTheme.dark.primaryTextTheme.apply(fontFamily: 'Roboto'),
);
