import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/main.dart';
import 'package:enote_desktop/shell/login_screen.dart';

void main() {
  testWidgets('App launches to login screen', (WidgetTester tester) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
    );

    await tester.pumpWidget(MyApp(authState: authState, apiClient: apiClient));

    expect(find.byType(LoginScreen), findsOneWidget);
  });
}
