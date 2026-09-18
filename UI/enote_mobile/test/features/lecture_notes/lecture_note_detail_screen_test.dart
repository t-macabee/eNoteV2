import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/lecture_notes/lecture_note_detail_screen.dart';
import 'package:enote_mobile/features/lecture_notes/lecture_note_provider.dart';

import '../../helpers.dart';

void main() {
  testWidgets('LectureNoteDetailScreen retries fetch on button tap after error',
      (WidgetTester tester) async {
    final noteJson = jsonEncode({
      'id': 4,
      'lectureId': 11,
      'title': 'Skale i akordi',
      'content': 'C-dur skala kroz dvije oktave.',
    });

    var attempts = 0;
    final client = ScriptedClient((_) {
      attempts++;
      if (attempts == 1) {
        return jsonResponse(
          jsonEncode({'detail': 'Server error'}),
          500,
          headers: {'content-type': 'application/problem+json'},
        );
      }
      return jsonResponse(noteJson, 200);
    });
    final authState = AuthState(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      httpClient: client,
    );
    final apiClient = ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );
    final provider = LectureNoteProvider(
      apiClient: apiClient,
      lectureId: 11,
    );

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<LectureNoteProvider>.value(
          value: provider,
          child: const LectureNoteDetailScreen(noteId: 4),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Pokušaj ponovo'), findsOneWidget);
    expect(find.text('Skale i akordi'), findsNothing);

    await tester.tap(find.text('Pokušaj ponovo'));
    await tester.pumpAndSettle();

    expect(find.text('Skale i akordi'), findsOneWidget);
    expect(find.text('C-dur skala kroz dvije oktave.'), findsOneWidget);
  });
}
