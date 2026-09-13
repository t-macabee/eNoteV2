import 'package:enote_core/enote_core.dart' as core;

String lectureStatusLabel(core.LectureDto lecture) {
  if (lecture.isCancelled) {
    return core.lectureStatusLabel(core.LectureStatus.cancelled);
  }
  return core.lectureStatusLabel(lecture.lectureStatus);
}
