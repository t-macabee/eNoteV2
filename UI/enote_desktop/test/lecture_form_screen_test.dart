import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_form_screen.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';

class _StubClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    return http.StreamedResponse(
      Stream.value(utf8.encode('{}')),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('F3-08: cancelled lecture disables Save', (tester) async {
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: _StubClient(),
    );
    final lectureProvider = LectureProvider(apiClient: apiClient);

    final cancelled = LectureDto(
      id: 3,
      name: 'Otkazano',
      location: 'Sala 1',
      lectureType: LectureType.theoretical,
      lectureStatus: LectureStatus.cancelled,
      isCancelled: true,
      lectureTime: DateTime(2026, 10, 1, 10),
      duration: 90,
      attendeeCount: 0,
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<LectureProvider>.value(
        value: lectureProvider,
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () {
                  EntityFormScaffold.showAsDialog(
                    context,
                    builder: (_) => LectureFormScreen(
                      courseId: 1,
                      existing: cancelled,
                      presentation: EntityFormPresentation.dialog,
                    ),
                  );
                },
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();

    expect(find.text('Ovo predavanje je otkazano i ne može se uređivati.'),
        findsOneWidget);

    final save = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Sačuvaj'),
    );
    expect(save.onPressed, isNull);
  });
}
