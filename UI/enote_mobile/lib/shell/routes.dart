import 'package:enote_core/enote_core.dart';

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
  final int announcementId;

  const AnnouncementDetailArgs(this.announcementId);
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

class AppRoutes {
  AppRoutes._();

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
}
