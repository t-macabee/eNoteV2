import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/widgets/form_submit_state.dart';

class _Probe extends StatefulWidget {
  final Future<void> Function() action;
  final Future<void> Function() withAction;
  final Future<void> Function()? bannerAction;

  const _Probe({
    required this.action,
    required this.withAction,
    this.bannerAction,
  });

  @override
  State<_Probe> createState() => _ProbeState();
}

class _ProbeState extends State<_Probe> with FormSubmitState<_Probe> {
  bool altBusy = false;

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.ltr,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(isSubmitting ? 'busy' : 'idle'),
          Text(altBusy ? 'alt-busy' : 'alt-idle'),
          Text(submitError ?? 'no-error'),
          TextButton(
            onPressed: () => submit(widget.action),
            child: const Text('run'),
          ),
          TextButton(
            onPressed: () =>
                submitWith((busy) => altBusy = busy, widget.withAction),
            child: const Text('run-alt'),
          ),
          if (widget.bannerAction != null)
            TextButton(
              onPressed: () => submitWith(
                (busy) => altBusy = busy,
                widget.bannerAction!,
                showBanner: true,
              ),
              child: const Text('run-banner'),
            ),
        ],
      ),
    );
  }
}

void main() {
  testWidgets('raises the busy flag while running and clears it after',
      (tester) async {
    final completer = Completer<void>();
    await tester.pumpWidget(
      _Probe(action: () => completer.future, withAction: () async {}),
    );

    await tester.tap(find.text('run'));
    await tester.pump();
    expect(find.text('busy'), findsOneWidget);
    expect(find.text('idle'), findsNothing);

    completer.complete();
    await tester.pumpAndSettle();
    expect(find.text('idle'), findsOneWidget);
    expect(find.text('no-error'), findsOneWidget);
  });

  testWidgets('stores the mapped message of a failed submit', (tester) async {
    await tester.pumpWidget(
      _Probe(
        action: () async => throw ApiException('Pogrešna lozinka.'),
        withAction: () async {},
      ),
    );

    await tester.tap(find.text('run'));
    await tester.pumpAndSettle();

    expect(find.text('Pogrešna lozinka.'), findsOneWidget);
    expect(find.text('idle'), findsOneWidget);
  });

  testWidgets('a later successful submit clears the error', (tester) async {
    var shouldFail = true;
    await tester.pumpWidget(
      _Probe(
        action: () async {
          if (shouldFail) throw ApiException('Greška.');
        },
        withAction: () async {},
      ),
    );

    await tester.tap(find.text('run'));
    await tester.pumpAndSettle();
    expect(find.text('Greška.'), findsOneWidget);

    shouldFail = false;
    await tester.tap(find.text('run'));
    await tester.pumpAndSettle();
    expect(find.text('no-error'), findsOneWidget);
  });

  testWidgets('submitWith raises the alternate flag, not isSubmitting',
      (tester) async {
    final completer = Completer<void>();
    await tester.pumpWidget(
      _Probe(action: () async {}, withAction: () => completer.future),
    );

    await tester.tap(find.text('run-alt'));
    await tester.pump();
    expect(find.text('alt-busy'), findsOneWidget);
    expect(find.text('busy'), findsNothing);

    completer.complete();
    await tester.pumpAndSettle();
    expect(find.text('alt-idle'), findsOneWidget);
  });

  testWidgets('showBanner routes the error to a SnackBar, not submitError',
      (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: _Probe(
            action: () async {},
            withAction: () async {},
            bannerAction: () async => throw ApiException('Greška.'),
          ),
        ),
      ),
    );

    await tester.tap(find.text('run-banner'));
    await tester.pumpAndSettle();

    expect(find.text('Greška.'), findsOneWidget);
    expect(find.text('no-error'), findsOneWidget);
    expect(find.text('alt-idle'), findsOneWidget);
  });
}
