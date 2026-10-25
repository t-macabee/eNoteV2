import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../features/announcements/announcement_detail_screen.dart';
import '../features/assignments/assignment_detail_screen.dart';
import '../features/assignments/submission_history_screen.dart';
import '../features/auth/forgot_password_screen.dart';
import '../features/auth/register_screen.dart';
import '../features/auth/reset_password_screen.dart';
import '../features/courses/course_detail_screen.dart';
import '../features/events/event_detail_screen.dart';
import '../features/instruments/instrument_detail_screen.dart';
import '../features/lecture_notes/lecture_note_detail_screen.dart';
import '../features/lecture_notes/lecture_note_list_screen.dart';
import '../features/lecture_notes/lecture_note_provider.dart';
import '../features/lectures/lecture_detail_screen.dart';
import '../features/notifications/notification_inbox_screen.dart';
import '../features/profile/change_password_screen.dart';
import '../features/profile/edit_profile_screen.dart';
import '../features/ranking/ranking_screen.dart';
import '../features/rentals/payments/rental_payment_screen.dart';
import '../features/rentals/rental_detail_screen.dart';
import '../features/tuition/tuition_history_screen.dart';
import '../features/tuition/tuition_payment_screen.dart';

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

  final int? courseId;

  const AssignmentDetailArgs(this.assignmentId, {this.courseId});
}

class RankingArgs {
  final int courseId;

  const RankingArgs(this.courseId);
}

class AnnouncementDetailArgs {
  final AnnouncementDto announcement;

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

class TuitionArgs {
  final int enrollmentId;
  final String courseName;
  final double price;

  const TuitionArgs(
    this.enrollmentId, {
    required this.courseName,
    required this.price,
  });
}

class TuitionHistoryArgs {
  final int enrollmentId;
  final String courseName;

  const TuitionHistoryArgs(
    this.enrollmentId, {
    required this.courseName,
  });
}

class ResetArgs {
  final String? email;
  final String? token;

  const ResetArgs({this.email, this.token});
}

class EventDetailArgs {
  final EventDto event;

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
  static const String tuitionPayment = '/tuition/pay';
  static const String tuitionHistory = '/tuition/history';
  static const String notifications = '/notifications';
  static const String profileEdit = '/profile/edit';
  static const String profilePassword = '/profile/password';
  static const String register = '/auth/register';
  static const String forgotPassword = '/auth/forgot';
  static const String resetPassword = '/auth/reset';
  static const String eventDetail = '/events/detail';

  static Route<dynamic> onGenerateRoute(RouteSettings settings) {
    return switch ((settings.name, settings.arguments)) {
      (courseDetail, CourseDetailArgs a) => _route(
        CourseDetailScreen(courseId: a.courseId),
      ),
      (lectureDetail, LectureDetailArgs a) => _route(
        LectureDetailScreen(lectureId: a.lectureId),
      ),
      (lectureNotes, LectureNotesArgs a) => _route(
        ChangeNotifierProvider(
          create: (context) => LectureNoteProvider(
            apiClient: context.read<ApiClient>(),
            lectureId: a.lectureId,
          ),
          child: LectureNoteListScreen(lectureId: a.lectureId),
        ),
      ),
      (lectureNoteDetail, LectureNoteDetailArgs a) => _route(
        ChangeNotifierProvider(
          create: (context) => LectureNoteProvider(
            apiClient: context.read<ApiClient>(),
            lectureId: a.lectureId,
          ),
          child: LectureNoteDetailScreen(noteId: a.noteId),
        ),
      ),
      (assignmentDetail, AssignmentDetailArgs a) => _route(
        AssignmentDetailScreen(
          assignmentId: a.assignmentId,
          courseId: a.courseId,
        ),
      ),
      (assignmentHistory, _) => _route(const SubmissionHistoryScreen()),
      (ranking, RankingArgs a) => _route(
        RankingScreen(courseId: a.courseId),
      ),
      (announcementDetail, AnnouncementDetailArgs a) => _route(
        AnnouncementDetailScreen(announcement: a.announcement),
      ),
      (instrumentDetail, InstrumentDetailArgs a) => _route(
        InstrumentDetailScreen(instrumentId: a.instrumentId),
      ),
      (rentalDetail, RentalDetailArgs a) => _route(
        RentalDetailScreen(rentalId: a.rentalId),
      ),
      (payment, PaymentArgs a) => _route(
        RentalPaymentScreen(rentalId: a.rentalId),
      ),
      (tuitionPayment, TuitionArgs a) => _route(
        TuitionPaymentScreen(
          enrollmentId: a.enrollmentId,
          courseName: a.courseName,
          price: a.price,
        ),
      ),
      (tuitionHistory, TuitionHistoryArgs a) => _route(
        TuitionHistoryScreen(
          enrollmentId: a.enrollmentId,
          courseName: a.courseName,
        ),
      ),
      (notifications, _) => _route(const NotificationInboxScreen()),
      (profileEdit, _) => _route(const EditProfileScreen()),
      (profilePassword, _) => _route(const ChangePasswordScreen()),
      (register, _) => _route(const RegisterScreen()),
      (forgotPassword, _) => _route(const ForgotPasswordScreen()),
      (resetPassword, ResetArgs a) => _route(
        ResetPasswordScreen(email: a.email, token: a.token),
      ),
      (resetPassword, _) => _route(const ResetPasswordScreen()),
      (eventDetail, EventDetailArgs a) => _route(
        EventDetailScreen(event: a.event),
      ),
      _ => _route(const _UnknownRouteScreen()),
    };
  }

  static MaterialPageRoute<dynamic> _route(Widget child) {
    return MaterialPageRoute<dynamic>(builder: (_) => child);
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
