/// The API serialises every enum as its PascalCase member name
/// (`JsonStringEnumConverter`, no naming policy). Dart member names are the
/// same words in camelCase, so the wire form is derived, not tabulated.
String _wireName(Enum value) =>
    value.name[0].toUpperCase() + value.name.substring(1);

T _fromWireName<T extends Enum>(List<T> values, String? json, T fallback) =>
    values.firstWhere((value) => _wireName(value) == json, orElse: () => fallback);

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

  String toJson() => _wireName(this);

  static LectureType fromJson(String? json) =>
      _fromWireName(values, json, LectureType.theoretical);
}

enum LectureStatus {
  scheduled,
  held,
  cancelled;

  String toJson() => _wireName(this);

  static LectureStatus fromJson(String? json) =>
      _fromWireName(values, json, LectureStatus.scheduled);
}

enum AttendanceStatus {
  pending,
  present,
  absent;

  String toJson() => _wireName(this);

  static AttendanceStatus fromJson(String? json) =>
      _fromWireName(values, json, AttendanceStatus.pending);
}

enum InstrumentRentalStatus {
  pending,
  approved,
  active,
  completed,
  rejected,
  canceled,
  returnedEarly;

  String toJson() => _wireName(this);

  static InstrumentRentalStatus fromJson(String? json) =>
      _fromWireName(values, json, InstrumentRentalStatus.pending);
}

enum EnrollmentStatus {
  active,
  completed,
  canceled,
  pending,
  rejected;

  String toJson() => _wireName(this);

  static EnrollmentStatus fromJson(String? json) =>
      _fromWireName(values, json, EnrollmentStatus.pending);
}

enum AnnouncementScope {
  course,
  musicStore;

  String toJson() => _wireName(this);

  static AnnouncementScope fromJson(String? json) =>
      _fromWireName(values, json, AnnouncementScope.course);
}

enum PaymentStatus {
  requiresAction,
  succeeded,
  failed,
  canceled,
  refunded,
  partiallyRefunded;

  String toJson() => _wireName(this);

  static PaymentStatus fromJson(String? json) =>
      _fromWireName(values, json, PaymentStatus.requiresAction);
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
