import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_mobile/session/deep_link_handler.dart';
import 'package:enote_mobile/shell/app_router.dart';

void main() {
  test('reset link with encoded + and / in the token parses', () {
    final target = DeepLinkHandler.parse(
      Uri.parse(
        'enote://reset-password?email=student%40enote.com&token=a%2Bb%2Fc%3D',
      ),
    );
    expect(target, isA<ResetPasswordTarget>());
    final reset = target as ResetPasswordTarget;
    expect(reset.email, 'student@enote.com');
    expect(reset.token, 'a+b/c=');
  });

  test('reset link with a missing token routes to ResetPasswordTarget', () {
    final withEmail = DeepLinkHandler.parse(
      Uri.parse('enote://reset-password?email=student%40enote.com'),
    );
    expect(withEmail, isA<ResetPasswordTarget>());
    final r1 = withEmail as ResetPasswordTarget;
    expect(r1.email, 'student@enote.com');
    expect(r1.token, isNull);

    final bare = DeepLinkHandler.parse(Uri.parse('enote://reset-password'));
    expect(bare, isA<ResetPasswordTarget>());
    final r2 = bare as ResetPasswordTarget;
    expect(r2.email, isNull);
    expect(r2.token, isNull);
  });

  test('stripe redirect parses to stripeRedirect', () {
    expect(
      DeepLinkHandler.parse(Uri.parse('enote://stripe-redirect')),
      isA<StripeRedirectTarget>(),
    );
  });

  test('other schemes and hosts are unknown', () {
    expect(
      DeepLinkHandler.parse(Uri.parse('https://enote.com/reset-password')),
      isA<UnknownTarget>(),
    );
    expect(
      DeepLinkHandler.parse(Uri.parse('enote://x')),
      isA<UnknownTarget>(),
    );
  });

  testWidgets('a link before the navigator exists is queued and flushed once',
      (tester) async {
    final navigatorKey = GlobalKey<NavigatorState>();
    final handler = DeepLinkHandler();
    handler.onLink(
      Uri.parse('enote://reset-password?email=s%40e.com&token=abc'),
      navigatorKey,
    );
    expect(handler.pendingTarget, isA<ResetPasswordTarget>());

    var builds = 0;
    await tester.pumpWidget(
      MaterialApp(
        navigatorKey: navigatorKey,
        onGenerateRoute: (settings) {
          builds++;
          final args = settings.arguments as ResetArgs?;
          return MaterialPageRoute(
            builder: (_) => Text('reset:${args?.email}'),
          );
        },
        home: const Text('home'),
      ),
    );
    handler.flush(navigatorKey);
    await tester.pumpAndSettle();
    expect(find.text('reset:s@e.com'), findsOneWidget);
    expect(handler.pendingTarget, isNull);

    handler.flush(navigatorKey);
    await tester.pumpAndSettle();
    expect(builds, 1);
  });
}
