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
import 'routes.dart';

class AppRouter {
  AppRouter._();

  static Route<dynamic> onGenerateRoute(RouteSettings settings) {
    return switch ((settings.name, settings.arguments)) {
      (AppRoutes.courseDetail, CourseDetailArgs a) => _route(
        CourseDetailScreen(courseId: a.courseId),
      ),
      (AppRoutes.lectureDetail, LectureDetailArgs a) => _route(
        LectureDetailScreen(lectureId: a.lectureId),
      ),
      (AppRoutes.lectureNotes, LectureNotesArgs a) => _route(
        ChangeNotifierProvider(
          create: (context) => LectureNoteProvider(
            apiClient: context.read<ApiClient>(),
            lectureId: a.lectureId,
          ),
          child: LectureNoteListScreen(lectureId: a.lectureId),
        ),
      ),
      (AppRoutes.lectureNoteDetail, LectureNoteDetailArgs a) => _route(
        ChangeNotifierProvider(
          create: (context) => LectureNoteProvider(
            apiClient: context.read<ApiClient>(),
            lectureId: a.lectureId,
          ),
          child: LectureNoteDetailScreen(noteId: a.noteId),
        ),
      ),
      (AppRoutes.assignmentDetail, AssignmentDetailArgs a) => _route(
        AssignmentDetailScreen(
          assignmentId: a.assignmentId,
          courseId: a.courseId,
        ),
      ),
      (AppRoutes.assignmentHistory, _) => _route(
        const SubmissionHistoryScreen(),
      ),
      (AppRoutes.ranking, RankingArgs a) => _route(
        RankingScreen(courseId: a.courseId),
      ),
      (AppRoutes.announcementDetail, AnnouncementDetailArgs a) => _route(
        AnnouncementDetailScreen(announcementId: a.announcementId),
      ),
      (AppRoutes.instrumentDetail, InstrumentDetailArgs a) => _route(
        InstrumentDetailScreen(instrumentId: a.instrumentId),
      ),
      (AppRoutes.rentalDetail, RentalDetailArgs a) => _route(
        RentalDetailScreen(rentalId: a.rentalId),
      ),
      (AppRoutes.payment, PaymentArgs a) => _route(
        RentalPaymentScreen(rentalId: a.rentalId),
      ),
      (AppRoutes.tuitionPayment, TuitionArgs a) => _route(
        TuitionPaymentScreen(
          enrollmentId: a.enrollmentId,
          courseName: a.courseName,
          price: a.price,
        ),
      ),
      (AppRoutes.tuitionHistory, TuitionHistoryArgs a) => _route(
        TuitionHistoryScreen(
          enrollmentId: a.enrollmentId,
          courseName: a.courseName,
        ),
      ),
      (AppRoutes.notifications, _) => _route(const NotificationInboxScreen()),
      (AppRoutes.profileEdit, _) => _route(const EditProfileScreen()),
      (AppRoutes.profilePassword, _) => _route(const ChangePasswordScreen()),
      (AppRoutes.register, _) => _route(const RegisterScreen()),
      (AppRoutes.forgotPassword, _) => _route(const ForgotPasswordScreen()),
      (AppRoutes.resetPassword, ResetArgs a) => _route(
        ResetPasswordScreen(email: a.email, token: a.token),
      ),
      (AppRoutes.resetPassword, _) => _route(const ResetPasswordScreen()),
      (AppRoutes.eventDetail, EventDetailArgs a) => _route(
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
