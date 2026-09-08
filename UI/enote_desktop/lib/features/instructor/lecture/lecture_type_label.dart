import 'package:enote_core/enote_core.dart';

String lectureTypeLabel(LectureType type) => switch (type) {
      LectureType.theoretical => 'Teorijsko',
      LectureType.practical => 'Praktično',
      LectureType.combined => 'Kombinovano',
    };

String lectureStatusLabel(LectureDto lecture) {
  if (lecture.isCancelled) return 'Otkazano';
  return switch (lecture.lectureStatus) {
    LectureStatus.scheduled => 'Zakazano',
    LectureStatus.held => 'Održano',
    LectureStatus.cancelled => 'Otkazano',
  };
}
