import 'package:enote_core/enote_core.dart';

class UserListItem {
  final int appUserId;
  final String displayName;
  final String? username;
  final String? firstName;
  final String? lastName;
  final UserRole role;
  final String? storeName;
  final DateTime? membershipPaidUntil;
  final DateTime? enrollmentDate;
  final bool? isManager;
  final bool? isActive;

  UserListItem.fromInstructor(InstructorDto i)
      : appUserId = i.appUserId,
        displayName = formatDisplayName(i.firstName, i.lastName, i.username),
        username = i.username,
        firstName = i.firstName,
        lastName = i.lastName,
        role = UserRole.instructor,
        storeName = null,
        membershipPaidUntil = null,
        enrollmentDate = null,
        isManager = null,
        isActive = i.isActive;

  UserListItem.fromStudent(StudentDto s)
      : appUserId = s.appUserId,
        displayName = formatDisplayName(s.firstName, s.lastName, s.username),
        username = s.username,
        firstName = s.firstName,
        lastName = s.lastName,
        role = UserRole.student,
        storeName = null,
        membershipPaidUntil = s.membershipPaidUntil,
        enrollmentDate = s.enrollmentDate,
        isManager = null,
        isActive = s.isActive;

  UserListItem.fromEmployee(ShopEmployeeDto e)
      : appUserId = e.appUserId,
        displayName = formatDisplayName(e.firstName, e.lastName, e.username),
        username = e.username,
        firstName = e.firstName,
        lastName = e.lastName,
        role = UserRole.storeEmployee,
        storeName = e.storeName,
        membershipPaidUntil = null,
        enrollmentDate = null,
        isManager = e.isManager,
        isActive = e.isActive;
}
