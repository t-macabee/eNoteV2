enum UserRole {
  administrator,
  instructor,
  student,
  storeEmployee;

  String get label => switch (this) {
    UserRole.administrator => 'Administrator',
    UserRole.instructor => 'Instructor',
    UserRole.student => 'Student',
    UserRole.storeEmployee => 'StoreEmployee',
  };

  static UserRole? fromString(String? value) {
    if (value == null) return null;
    return switch (value) {
      'Administrator' => UserRole.administrator,
      'Instructor' => UserRole.instructor,
      'Student' => UserRole.student,
      'StoreEmployee' => UserRole.storeEmployee,
      _ => null,
    };
  }
}

enum LectureType {
  theoretical,
  practical,
  combined;

  String toJson() => switch (this) {
    LectureType.theoretical => 'Theoretical',
    LectureType.practical => 'Practical',
    LectureType.combined => 'Combined',
  };

  static LectureType fromJson(String json) => switch (json) {
    'Theoretical' => LectureType.theoretical,
    'Practical' => LectureType.practical,
    'Combined' => LectureType.combined,
    _ => LectureType.theoretical,
  };
}

enum LectureStatus {
  scheduled,
  held,
  cancelled;

  int toJson() => index + 1;

  static LectureStatus fromJson(int json) => switch (json) {
    1 => LectureStatus.scheduled,
    2 => LectureStatus.held,
    3 => LectureStatus.cancelled,
    _ => LectureStatus.scheduled,
  };
}

enum AttendanceStatus {
  pending,
  present,
  absent;

  int toJson() => index + 1;

  static AttendanceStatus fromJson(int json) => switch (json) {
    1 => AttendanceStatus.pending,
    2 => AttendanceStatus.present,
    3 => AttendanceStatus.absent,
    _ => AttendanceStatus.pending,
  };
}

enum InstrumentRentalStatus {
  pending,
  approved,
  active,
  completed,
  rejected,
  canceled,
  returnedEarly;

  int toJson() => index + 1;

  static InstrumentRentalStatus fromJson(int json) => switch (json) {
    1 => InstrumentRentalStatus.pending,
    2 => InstrumentRentalStatus.approved,
    3 => InstrumentRentalStatus.active,
    4 => InstrumentRentalStatus.completed,
    5 => InstrumentRentalStatus.rejected,
    6 => InstrumentRentalStatus.canceled,
    7 => InstrumentRentalStatus.returnedEarly,
    _ => InstrumentRentalStatus.pending,
  };
}

enum AnnouncementScope {
  course,
  musicStore;

  String toJson() => switch (this) {
    AnnouncementScope.course => 'Course',
    AnnouncementScope.musicStore => 'MusicStore',
  };

  static AnnouncementScope fromJson(String json) => switch (json) {
    'Course' => AnnouncementScope.course,
    'MusicStore' => AnnouncementScope.musicStore,
    _ => AnnouncementScope.course,
  };
}

enum PaymentStatus {
  requiresAction,
  succeeded,
  failed,
  canceled,
  refunded,
  partiallyRefunded;

  String toJson() => switch (this) {
    PaymentStatus.requiresAction => 'RequiresAction',
    PaymentStatus.succeeded => 'Succeeded',
    PaymentStatus.failed => 'Failed',
    PaymentStatus.canceled => 'Canceled',
    PaymentStatus.refunded => 'Refunded',
    PaymentStatus.partiallyRefunded => 'PartiallyRefunded',
  };

  static PaymentStatus fromJson(String json) => switch (json) {
    'RequiresAction' => PaymentStatus.requiresAction,
    'Succeeded' => PaymentStatus.succeeded,
    'Failed' => PaymentStatus.failed,
    'Canceled' => PaymentStatus.canceled,
    'Refunded' => PaymentStatus.refunded,
    'PartiallyRefunded' => PaymentStatus.partiallyRefunded,
    _ => PaymentStatus.requiresAction,
  };
}

enum RentalTrigger {
  approve,
  reject,
  pickup,
  complete,
  cancel,
  returnEarly;

  int toJson() => index;

  static RentalTrigger fromJson(int json) => switch (json) {
    0 => RentalTrigger.approve,
    1 => RentalTrigger.reject,
    2 => RentalTrigger.pickup,
    3 => RentalTrigger.complete,
    4 => RentalTrigger.cancel,
    5 => RentalTrigger.returnEarly,
    _ => RentalTrigger.approve,
  };
}
