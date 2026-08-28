import 'package:enote_core/enote_core.dart';

String lectureTypeLabel(LectureType type) => switch (type) {
      LectureType.theoretical => 'Teorijsko',
      LectureType.practical => 'Praktično',
      LectureType.combined => 'Kombinovano',
    };
