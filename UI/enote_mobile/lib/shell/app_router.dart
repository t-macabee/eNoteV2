import 'package:flutter/material.dart';

import '../features/auth/forgot_password_screen.dart';
import '../features/auth/register_screen.dart';
import '../features/auth/reset_password_screen.dart';
import '../features/profile/change_password_screen.dart';
import '../features/profile/edit_profile_screen.dart';

class CourseDetailArgs {
  final int courseId;

  const CourseDetailArgs(this.courseId);
}

class LectureDetailArgs {
  final int lectureId;

  const LectureDetailArgs(this.lectureId);
}

class LectureNotesArgs {
  final int lectureId;

  const LectureNotesArgs(this.lectureId);
}

class LectureNoteDetailArgs {
  final int lectureId;
  final int noteId;

  const LectureNoteDetailArgs(this.lectureId, this.noteId);
}

class AssignmentDetailArgs {
  final int assignmentId;

  const AssignmentDetailArgs(this.assignmentId);
}

class RankingArgs {
  final int courseId;

  const RankingArgs(this.courseId);
}

class AnnouncementDetailArgs {
  final Object announcement;

  const AnnouncementDetailArgs(this.announcement);
}

class InstrumentDetailArgs {
  final int instrumentId;

  const InstrumentDetailArgs(this.instrumentId);
}

class RentalDetailArgs {
  final int rentalId;

  const RentalDetailArgs(this.rentalId);
}

class PaymentArgs {
  final int rentalId;

  const PaymentArgs(this.rentalId);
}

class ResetArgs {
  final String? email;
  final String? token;

  const ResetArgs({this.email, this.token});
}

class EventDetailArgs {
  final Object event;

  const EventDetailArgs(this.event);
}

class AppRouter {
  AppRouter._();

  static const String courseDetail = '/courses/detail';
  static const String lectureDetail = '/lectures/detail';
  static const String lectureNotes = '/lectures/notes';
  static const String lectureNoteDetail = '/lectures/notes/detail';
  static const String assignmentDetail = '/assignments/detail';
  static const String assignmentHistory = '/assignments/history';
  static const String ranking = '/courses/ranking';
  static const String announcementDetail = '/announcements/detail';
  static const String instrumentDetail = '/instruments/detail';
  static const String rentalDetail = '/rentals/detail';
  static const String payment = '/rentals/pay';
  static const String notifications = '/notifications';
  static const String profileEdit = '/profile/edit';
  static const String profilePassword = '/profile/password';
  static const String register = '/auth/register';
  static const String forgotPassword = '/auth/forgot';
  static const String resetPassword = '/auth/reset';
  static const String eventDetail = '/events/detail';

  static Route<dynamic> onGenerateRoute(RouteSettings settings) {
    final args = settings.arguments;
    switch (settings.name) {
      case courseDetail:
        return _route(
          _PlaceholderScreen(title: _courseTitle(args as CourseDetailArgs?)),
        );
      case lectureDetail:
        return _route(const _PlaceholderScreen(title: 'Predavanje'));
      case lectureNotes:
        return _route(const _PlaceholderScreen(title: 'Bilješke'));
      case lectureNoteDetail:
        return _route(const _PlaceholderScreen(title: 'Bilješka'));
      case assignmentDetail:
        return _route(const _PlaceholderScreen(title: 'Zadatak'));
      case assignmentHistory:
        return _route(const _PlaceholderScreen(title: 'Moje predaje'));
      case ranking:
        return _route(const _PlaceholderScreen(title: 'Rang lista'));
      case announcementDetail:
        return _route(const _PlaceholderScreen(title: 'Objava'));
      case instrumentDetail:
        return _route(const _PlaceholderScreen(title: 'Instrument'));
      case rentalDetail:
        return _route(const _PlaceholderScreen(title: 'Iznajmljivanje'));
      case payment:
        return _route(const _PlaceholderScreen(title: 'Plaćanje'));
      case notifications:
        return _route(const _PlaceholderScreen(title: 'Obavještenja'));
      case profileEdit:
        return _route(const EditProfileScreen());
      case profilePassword:
        return _route(const ChangePasswordScreen());
      case register:
        return _route(const RegisterScreen());
      case forgotPassword:
        return _route(const ForgotPasswordScreen());
      case resetPassword:
        final resetArgs = args is ResetArgs ? args : null;
        return _route(
          ResetPasswordScreen(
            email: resetArgs?.email,
            token: resetArgs?.token,
          ),
        );
      case eventDetail:
        return _route(const _PlaceholderScreen(title: 'Događaj'));
      default:
        return _route(const _UnknownRouteScreen());
    }
  }

  static String _courseTitle(CourseDetailArgs? args) => 'Kurs';

  static MaterialPageRoute<dynamic> _route(Widget child) {
    return MaterialPageRoute<dynamic>(builder: (_) => child);
  }
}

class _PlaceholderScreen extends StatelessWidget {
  final String title;

  const _PlaceholderScreen({required this.title});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: Center(child: Text(title)),
    );
  }
}

class _UnknownRouteScreen extends StatelessWidget {
  const _UnknownRouteScreen();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(),
      body: Center(
        child: FilledButton(
          onPressed: () => Navigator.of(context).maybePop(),
          child: const Text('Pokušaj ponovo'),
        ),
      ),
    );
  }
}
