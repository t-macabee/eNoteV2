import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/theme/app_theme.dart';
import 'package:enote_mobile/widgets/app_sheet_body.dart';
import 'package:enote_mobile/widgets/detail_actions.dart';
import 'package:enote_mobile/widgets/detail_header.dart';
import 'package:enote_mobile/widgets/submit_button.dart';

import '../helpers.dart';

Widget _host(Widget child) {
  return MaterialApp(
    theme: mobileTheme,
    home: Scaffold(body: child),
  );
}

void main() {
  testWidgets('DetailHeader shows title, subtitle and chips', (tester) async {
    setPhoneSurface(tester);
    await tester.pumpWidget(
      _host(
        const DetailHeader(
          title: 'Yamaha C40',
          subtitle: 'Klasična gitara · Muzika d.o.o.',
          chips: [StatusChip(label: 'Dostupno', tone: StatusTone.success)],
        ),
      ),
    );

    expect(find.text('Yamaha C40'), findsOneWidget);
    expect(find.text('Klasična gitara · Muzika d.o.o.'), findsOneWidget);
    expect(find.text('Dostupno'), findsOneWidget);
    expect(tester.widget<Text>(find.text('Yamaha C40')).style?.fontSize, 24);
  });

  testWidgets('DetailHeader without subtitle or chips adds no gap', (
    tester,
  ) async {
    setPhoneSurface(tester);
    await tester.pumpWidget(_host(const DetailHeader(title: 'Lekcija')));

    expect(find.byType(Wrap), findsNothing);
    expect(find.byType(SizedBox), findsNothing);
  });

  testWidgets('DetailHeader with onTap is tappable', (tester) async {
    setPhoneSurface(tester);
    var taps = 0;
    await tester.pumpWidget(
      _host(DetailHeader(title: 'Instrument', onTap: () => taps++)),
    );

    await tester.tap(find.text('Instrument'));
    expect(taps, 1);
  });

  testWidgets('DetailActions stacks the actions at full width', (tester) async {
    setPhoneSurface(tester);
    await tester.pumpWidget(
      _host(
        DetailActions(
          actions: [
            FilledButton(onPressed: () {}, child: const Text('Prvo')),
            OutlinedButton(onPressed: () {}, child: const Text('Drugo')),
          ],
        ),
      ),
    );

    final first = tester.getSize(find.widgetWithText(FilledButton, 'Prvo'));
    final second = tester.getSize(find.widgetWithText(OutlinedButton, 'Drugo'));
    expect(first.width, second.width);
    expect(first.width, greaterThan(300));
    expect(
      tester.getTopLeft(find.widgetWithText(OutlinedButton, 'Drugo')).dy,
      greaterThan(
        tester.getBottomLeft(find.widgetWithText(FilledButton, 'Prvo')).dy,
      ),
    );
  });

  testWidgets('DetailActions renders nothing when empty', (tester) async {
    setPhoneSurface(tester);
    await tester.pumpWidget(_host(const DetailActions(actions: [])));

    expect(find.byType(Padding), findsNothing);
  });

  testWidgets('SubmitButton shows a spinner and is disabled while busy', (
    tester,
  ) async {
    setPhoneSurface(tester);
    var taps = 0;
    await tester.pumpWidget(
      _host(
        SubmitButton(label: 'Sačuvaj', onPressed: () => taps++, isBusy: true),
      ),
    );

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    expect(find.text('Sačuvaj'), findsNothing);
    expect(
      tester.widget<FilledButton>(find.byType(FilledButton)).onPressed,
      isNull,
    );
    expect(
      tester.getSize(find.byType(CircularProgressIndicator)),
      const Size.square(AppSize.spinnerMd),
    );
  });

  testWidgets('SubmitButton is enabled and shows its label when idle', (
    tester,
  ) async {
    setPhoneSurface(tester);
    var taps = 0;
    await tester.pumpWidget(
      _host(SubmitButton(label: 'Sačuvaj', onPressed: () => taps++)),
    );

    expect(find.byType(CircularProgressIndicator), findsNothing);
    await tester.tap(find.text('Sačuvaj'));
    expect(taps, 1);
  });

  testWidgets('AppSheetBody puts the title above the children', (tester) async {
    setPhoneSurface(tester);
    await tester.pumpWidget(
      _host(
        const AppSheetBody(
          title: 'Zahtjev',
          subtitle: 'Yamaha C40',
          children: [Text('Sadržaj')],
        ),
      ),
    );

    final title = tester.getTopLeft(find.text('Zahtjev')).dy;
    final subtitle = tester.getTopLeft(find.text('Yamaha C40')).dy;
    final child = tester.getTopLeft(find.text('Sadržaj')).dy;
    expect(title, lessThan(subtitle));
    expect(subtitle, lessThan(child));
    expect(tester.widget<Text>(find.text('Zahtjev')).style?.fontSize, 22);
  });
}
