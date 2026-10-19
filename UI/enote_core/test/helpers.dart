import 'package:enote_core/enote_core.dart';
export 'package:enote_test_support/enote_test_support.dart';

Future<void> pastDebounce() => Future<void>.delayed(
      PagedFetchController.defaultSearchDebounce +
          const Duration(milliseconds: 100),
    );
