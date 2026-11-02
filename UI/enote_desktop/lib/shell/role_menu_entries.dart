import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../features/admin/course/admin_course_list_screen.dart';
import '../features/admin/dashboard/admin_dashboard_screen.dart';
import '../features/admin/event/event_list_screen.dart';
import '../features/admin/music_store/music_store_list_screen.dart';
import '../features/admin/reference_data/reference_data_dialog.dart';
import '../features/admin/users/user_grid_screen.dart';
import '../features/instructor/course/course_list_screen.dart';
import '../features/instructor/course/course_catalog_screen.dart';
import '../features/instructor/dashboard/instructor_dashboard_screen.dart';
import '../features/instructor/enrollment/instructor_enrollment_list_screen.dart';
import '../features/instructor/student/instructor_student_list_screen.dart';
import '../features/instructor/student_directory/student_directory_screen.dart';
import '../features/shared/announcement/announcement_list_screen.dart';
import '../features/shared/announcement/store_announcement_provider.dart';
import '../features/store_employee/dashboard/employee_dashboard_screen.dart';
import '../features/store_employee/employee/shop_employee_list_screen.dart';
import '../features/store_employee/rental/rental_list_screen.dart';
import '../features/store_employee/store/shop_store_screen.dart';
import '../widgets/entity_list_screen.dart';
import 'role_menu.dart';

const kRequestsEntryLabel = 'Zahtjevi';

const kRoleMenuEntries = <RoleMenuEntry>[
  RoleMenuEntry(
    icon: Icons.dashboard_outlined,
    label: 'Početna',
    screenBuilder: _buildAdminDashboard,
    allowedRoles: [UserRole.administrator],
  ),
  RoleMenuEntry(
    icon: Icons.people_outline,
    label: 'Korisnici',
    screenBuilder: _buildUserGrid,
    allowedRoles: [UserRole.administrator],
  ),
  RoleMenuEntry(
    icon: Icons.store_outlined,
    label: 'Trgovina muzičke opreme',
    screenBuilder: _buildMusicStoreList,
    allowedRoles: [UserRole.administrator],
  ),
  RoleMenuEntry(
    icon: Icons.class_outlined,
    label: 'Kursevi',
    screenBuilder: _buildAdminCourseList,
    allowedRoles: [UserRole.administrator],
  ),
  RoleMenuEntry(
    icon: Icons.event_outlined,
    label: 'Događaji',
    screenBuilder: _buildEventList,
    allowedRoles: [UserRole.administrator],
  ),
  RoleMenuEntry(
    icon: Icons.dataset_outlined,
    label: 'Referentni podaci',
    screenBuilder: _buildReferenceDataDialog,
    allowedRoles: [UserRole.administrator],
    isDialog: true,
    dividerBefore: true,
  ),
  RoleMenuEntry(
    icon: Icons.dashboard_outlined,
    label: 'Početna',
    screenBuilder: _buildInstructorDashboard,
    allowedRoles: [UserRole.instructor],
  ),
  RoleMenuEntry(
    icon: Icons.class_outlined,
    label: 'Kursevi',
    screenBuilder: _buildCourseList,
    allowedRoles: [UserRole.instructor],
  ),
  RoleMenuEntry(
    icon: Icons.assignment_outlined,
    label: kRequestsEntryLabel,
    screenBuilder: _buildInstructorEnrollmentList,
    allowedRoles: [UserRole.instructor],
  ),
  RoleMenuEntry(
    icon: Icons.menu_book_outlined,
    label: 'Katalog kurseva',
    screenBuilder: _buildCourseCatalog,
    allowedRoles: [UserRole.instructor],
  ),
  RoleMenuEntry(
    icon: Icons.school_outlined,
    label: 'Moji studenti',
    screenBuilder: _buildInstructorStudentList,
    allowedRoles: [UserRole.instructor],
  ),
  RoleMenuEntry(
    icon: Icons.groups_outlined,
    label: 'Svi studenti',
    screenBuilder: _buildStudentDirectory,
    allowedRoles: [UserRole.instructor],
  ),
  RoleMenuEntry(
    icon: Icons.dashboard_outlined,
    label: 'Početna',
    screenBuilder: _buildEmployeeDashboard,
    allowedRoles: [UserRole.storeEmployee],
  ),
  RoleMenuEntry(
    icon: Icons.storefront_outlined,
    label: 'Moja prodavnica',
    screenBuilder: _buildShopStoreScreen,
    allowedRoles: [UserRole.storeEmployee],
  ),
  RoleMenuEntry(
    icon: Icons.badge_outlined,
    label: 'Zaposlenici',
    screenBuilder: _buildShopEmployeeList,
    allowedRoles: [UserRole.storeEmployee],
  ),
  RoleMenuEntry(
    icon: Icons.assignment_outlined,
    label: kRequestsEntryLabel,
    screenBuilder: _buildRentalList,
    allowedRoles: [UserRole.storeEmployee],
  ),
  RoleMenuEntry(
    icon: Icons.campaign_outlined,
    label: 'Objave',
    screenBuilder: _buildAnnouncementList,
    allowedRoles: [UserRole.storeEmployee],
  ),
];

Widget _buildAdminDashboard(BuildContext context) {
  return const AdminDashboardScreen();
}

Widget _buildInstructorDashboard(BuildContext context) {
  return const InstructorDashboardScreen();
}

Widget _buildEmployeeDashboard(BuildContext context) {
  return const EmployeeDashboardScreen();
}

Widget _buildMusicStoreList(BuildContext context) {
  return const MusicStoreListScreen();
}

Widget _buildCourseList(BuildContext context) {
  return const CourseListScreen();
}

Widget _buildCourseCatalog(BuildContext context) {
  return const CourseCatalogScreen();
}

Widget _buildInstructorEnrollmentList(BuildContext context) {
  return const InstructorEnrollmentListScreen();
}

Widget _buildInstructorStudentList(BuildContext context) {
  return const InstructorStudentListScreen();
}

Widget _buildStudentDirectory(BuildContext context) {
  return const StudentDirectoryScreen();
}

Widget _buildShopEmployeeList(BuildContext context) {
  return const ShopEmployeeListScreen();
}

Widget _buildUserGrid(BuildContext context) {
  return const UserGridScreen();
}

Widget _buildAdminCourseList(BuildContext context) {
  return const AdminCourseListScreen();
}

Widget _buildEventList(BuildContext context) {
  return const EventListScreen();
}

Widget _buildReferenceDataDialog(BuildContext context) {
  return const ReferenceDataDialog();
}

Widget _buildRentalList(BuildContext context) {
  return const RentalListScreen(presentation: EntityListPresentation.embedded);
}

Widget _buildAnnouncementList(BuildContext context) {
  return AnnouncementListScreen(
    provider: context.read<StoreAnnouncementProvider>(),
    presentation: EntityListPresentation.embedded,
  );
}

Widget _buildShopStoreScreen(BuildContext context) {
  return const ShopStoreScreen();
}
