import 'package:enote_core/enote_core.dart';

LectureStatus displayLectureStatus(LectureDto lecture) =>
    lecture.isCancelled ? LectureStatus.cancelled : lecture.lectureStatus;
