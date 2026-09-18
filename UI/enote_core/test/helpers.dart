import 'package:enote_core/enote_core.dart';
export 'package:enote_core/testing/testing.dart';

/// Long enough for the controller's search debounce to elapse, derived from
/// [PagedFetchController.defaultSearchDebounce] (+100 ms buffer for real-timer
/// unit tests) so the two cannot drift.
Future<void> pastDebounce() => Future<void>.delayed(
      PagedFetchController.defaultSearchDebounce +
          const Duration(milliseconds: 100),
    );
