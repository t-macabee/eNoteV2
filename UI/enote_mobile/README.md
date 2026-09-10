# enote_mobile

Flutter client for eNote students. Shares models, HTTP client and auth with the
desktop app through the `enote_core` package (path-referenced).

## Running

Emulator (the Android emulator reaches the host through `10.0.2.2`):

```bash
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5059/api/v1/ --dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_your_key
```

Physical device (same Wi-Fi as the PC; bind the API to all interfaces with
`dotnet run --project eNote.API --urls http://0.0.0.0:5059` or Docker, and add
the PC LAN IP to `android/app/src/main/res/xml/network_security_config.xml`):

```bash
flutter run -d <device-id> --dart-define=API_BASE_URL=http://192.168.1.20:5059/api/v1/ --dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_your_key
```

`API_BASE_URL` must include `/api/v1/`. Both values are read with
`String.fromEnvironment` in `lib/config.dart`, so neither is hardcoded.

Sign in with a Student account (`student` / `test`).

## Password reset deep link

```bash
adb shell am force-stop ba.enote.enote_mobile
adb shell am start -W -a android.intent.action.VIEW -d "enote://reset-password?email=student%40enote.com&token=<token>"
```

The same command with the app only backgrounded must open the existing task.

## Master–details

The mobile master–details screen is the *Kurs* detail (`CourseDetailScreen`):
course header plus its paged lectures; enrolling shows the lectures, unenrolling
hides them.

## Tests

```bash
flutter analyze && flutter test
```

## Release build

```bash
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5059/api/v1/ --dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_your_key
```

Output lands in `build/app/outputs/flutter-apk/app-release.apk`.
