import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/ranking/ranking_provider.dart';

void main() {
  test('F3-06: RankingProvider exposes the ranking report route', () {
    final provider = RankingProvider(
      apiClient: ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(),
      ),
    );

    expect(
      provider.reportEndpoint(7),
      'instructor/courses/7/ranking/report',
    );
  });
}
