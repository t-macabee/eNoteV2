import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_form_screen.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_list_screen.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';

import '../../../helpers.dart';

void main() {
  testWidgets('InstructorStudentListScreen displays student and opens form', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = DelegatedUserHttpClient();
    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => fakeJwt(username: 'testuser', role: 'Instructor'),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: mockClient,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<AuthState>.value(value: authState),
          ChangeNotifierProvider<InstructorStudentProvider>(
            create: (_) => InstructorStudentProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(home: InstructorStudentListScreen()),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Edin Dzeko'), findsOneWidget);
    expect(find.text('Kreiraj studenta'), findsOneWidget);

    await tester.tap(find.text('Kreiraj studenta'));
    await tester.pumpAndSettle();

    expect(find.byType(InstructorStudentFormScreen), findsOneWidget);

    await tester.enterText(find.byType(TextFormField).at(0), 'newstudent');
    await tester.enterText(
      find.byType(TextFormField).at(1),
      'student@example.com',
    );
    await tester.enterText(find.byType(TextFormField).at(2), 'Pass123!');
    await tester.enterText(find.byType(TextFormField).at(3), 'New');
    await tester.enterText(find.byType(TextFormField).at(4), 'Student');

    await tester.tap(find.text('Kreiraj'));
    await tester.pumpAndSettle();

    expect(mockClient.postUrls.single, endsWith('instructor/students'));
    expect(mockClient.postBodies.single, contains('"username":"newstudent"'));

    expect(find.text('Student je kreiran.'), findsOneWidget);
    expect(find.byType(InstructorStudentFormScreen), findsOneWidget);
  });

  testWidgets('400 response produces mapped Bosnian message', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = DelegatedUserHttpClient();
    mockClient.simulate400 = true;

    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => fakeJwt(username: 'testuser', role: 'Instructor'),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: mockClient,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<AuthState>.value(value: authState),
          ChangeNotifierProvider<InstructorStudentProvider>(
            create: (_) => InstructorStudentProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(home: InstructorStudentFormScreen()),
      ),
    );

    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextFormField).at(0), 'badstudent');
    await tester.enterText(find.byType(TextFormField).at(1), 'bad@example.com');
    await tester.enterText(find.byType(TextFormField).at(2), 'Pass123!');

    await tester.tap(find.text('Kreiraj'));
    await tester.pumpAndSettle();

    expect(find.text('Neispravan zahtjev.'), findsOneWidget);
  });

  testWidgets('Fetcher query map includes includeTotalCount for pagination', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1200, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockClient = DelegatedUserHttpClient();
    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: mockClient,
      tokenReader: () => fakeJwt(username: 'testuser', role: 'Instructor'),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: mockClient,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<AuthState>.value(value: authState),
          ChangeNotifierProvider<InstructorStudentProvider>(
            create: (_) => InstructorStudentProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(home: InstructorStudentListScreen()),
      ),
    );

    await tester.pumpAndSettle();

    expect(
      mockClient.getUrls.any((url) => url.contains('includeTotalCount=true')),
      isTrue,
    );
  });
}
