import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_form_screen.dart';
import 'package:enote_desktop/features/admin/city/city_form_screen.dart';
import 'package:enote_desktop/features/admin/event/event_form_screen.dart';
import 'package:enote_desktop/features/admin/instrument_type/instrument_type_form_screen.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_detail_screen.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_form_screen.dart';
import 'package:enote_desktop/features/admin/reference_data/reference_data_dialog.dart';
import 'package:enote_desktop/features/admin/users/user_details_dialog.dart';
import 'package:enote_desktop/features/admin/users/user_provision_form_screen.dart';
import 'package:enote_desktop/features/instructor/assignment/assignment_form_screen.dart';
import 'package:enote_desktop/features/instructor/course/course_catalog_detail_dialog.dart';
import 'package:enote_desktop/features/instructor/course/course_detail_screen.dart';
import 'package:enote_desktop/features/instructor/course/course_form_screen.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_form_screen.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_details_dialog.dart';
import 'package:enote_desktop/features/instructor/student_directory/student_profile_dialog.dart';
import 'package:enote_desktop/features/profile/change_password_dialog.dart';
import 'package:enote_desktop/features/profile/profile_dialog.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_detail_dialog.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_form_screen.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_form_screen.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_form_screen.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_detail_dialog.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_form_screen.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_screen.dart';
import 'package:enote_desktop/main.dart';
import 'package:enote_desktop/shell/master_screen.dart';
import 'package:enote_desktop/shell/role_menu.dart';
import 'package:enote_desktop/shell/role_menu_entries.dart';
import 'package:enote_desktop/widgets/store_profile_layout.dart';
import 'package:enote_desktop/widgets/store_profile_panel.dart';

import '../helpers.dart';

const Size _testWindowSize = Size(
  AppSize.minWindowWidth,
  AppSize.minWindowHeight,
);

Map<String, Object?> _record(int index) => {
  'id': index,
  'appUserId': 100 + index,
  'studentId': index,
  'studentProfileId': index,
  'studentUserId': 100 + index,
  'instructorId': 1,
  'musicStoreId': 1,
  'courseId': 1,
  'lectureId': 1,
  'instrumentId': 1,
  'instrumentTypeId': 1,
  'addressId': 1,
  'cityId': 1,
  'name': 'Naziv $index',
  'title': 'Naslov $index',
  'description': 'Opis zapisa $index',
  'content': 'Sadržaj zapisa $index',
  'firstName': 'Ime$index',
  'lastName': 'Prezime$index',
  'username': 'korisnik$index',
  'studentName': 'Ime$index Prezime$index',
  'instructorName': 'Instruktor Instruktorović',
  'courseName': 'Gitara za početnike',
  'storeName': 'Muzička prodavnica $index',
  'businessHours': '08:00 - 16:00',
  'phoneNumber': '+387 61 111 222',
  'addressStreet': 'Ferhadija',
  'addressCity': 'Sarajevo',
  'street': 'Ferhadija',
  'number': '15',
  'city': 'Sarajevo',
  'model': 'Stratocaster $index',
  'manufacturerId': 1, 'manufacturerName': 'Fender',
  'instrumentModel': 'Stratocaster $index',
  'instrumentType': 'Gitara',
  'musicStore': 'Muzička prodavnica $index',
  'type': 'Gitara',
  'location': 'Sala 1',
  'role': 'Student',
  'enrollmentStatus': 'Pending',
  'rentalStatus': 'Pending',
  'lectureType': 'Practical',
  'lectureStatus': 'Scheduled',
  'scope': 'MusicStore',
  'price': 120.0,
  'fee': 30.0,
  'monthlyFee': 50.0,
  'duration': 60,
  'enrolledCount': 5,
  'lectureCount': 3,
  'attendeeCount': 2,
  'isActive': true,
  'isPublished': true,
  'isManager': false,
  'isAvailable': true,
  'isPaid': true,
  'amountPaid': 120.0,
  'paidAt': '2026-11-01T10:00:00Z',
  'amountCents': 12000,
  'currency': 'BAM',
  'paymentIntentId': 'pi_1',
  'rentalId': 1,
  'refundedCents': 0,
  'status': 'Succeeded',
  'startDate': '2026-11-10T10:00:00Z',
  'endDate': '2027-02-10T10:00:00Z',
  'startsAt': '2026-11-10T10:00:00Z',
  'endsAt': '2026-11-10T12:00:00Z',
  'requestedAt': '2026-11-01T10:00:00Z',
  'publishedAt': '2026-11-01T10:00:00Z',
  'createdAt': '2026-11-01T10:00:00Z',
  'lectureTime': '2026-11-10T10:00:00Z',
  'dueAt': '2026-11-20T10:00:00Z',
  'enrollmentDate': '2026-09-01T10:00:00Z',
  'membershipPaidUntil': '2027-09-01T10:00:00Z',
};

ScriptedClient _client() {
  return ScriptedClient((request) {
    final url = request.url.toString();
    if (url.contains('notifications/unread-count')) {
      return jsonResponse({'unreadCount': 0}, 200);
    }
    if (url.contains('notifications')) {
      return jsonResponse(const {'items': [], 'unreadCount': 0}, 200);
    }
    if (url.contains('ranking')) {
      return jsonResponse([_record(1), _record(2)], 200);
    }
    return jsonResponse({
      ..._record(1),
      'profile': _record(1),
      'courses': [_record(1), _record(2)],
      'items': request.url.queryParameters['page'] == '1'
          ? [for (var i = 1; i <= 12; i++) _record(i)]
          : const [],
      'page': 1,
      'pageSize': 20,
      'totalCount': 45,
      'unreadCount': 0,
    }, 200);
  });
}

Widget _buildApp(String role) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(role: role, isManager: true),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: _client(),
  );
  return MyApp(authState: authState, apiClient: apiClient);
}

Future<void> _openMenuEntry(WidgetTester tester, String label) async {
  await tester.tap(
    find.descendant(of: find.byType(RoleMenu), matching: find.text(label)),
  );
  await tester.pumpAndSettle();
}

Future<void> _tapFirstCard(WidgetTester tester) async {
  await tester.tap(find.byType(Card).first);
  await tester.pumpAndSettle();
}

Future<void> _tapRowWithText(WidgetTester tester, String text) async {
  await tester.tap(find.text(text).first);
  await tester.pumpAndSettle();
}

Future<void> _tapAdd(WidgetTester tester) async {
  await tester.tap(find.byIcon(Icons.add).first);
  await tester.pumpAndSettle();
}

Future<void> _openUserMenuItem(WidgetTester tester, String label) async {
  await tester.tap(find.byIcon(Icons.unfold_more));
  await tester.pumpAndSettle();
  await tester.tap(find.text(label));
  await tester.pumpAndSettle();
}

Future<void> _tapTooltip(WidgetTester tester, String tooltip) async {
  final finder = find.byTooltip(tooltip).first;
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
  await tester.tap(finder);
  await tester.pumpAndSettle();
}

Future<void> _tapScrolledText(WidgetTester tester, String text) async {
  final finder = find.text(text).first;
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
  await tester.tap(finder);
  await tester.pumpAndSettle();
}

void _expectStoreProfileMounted(WidgetTester tester) {
  expect(find.byType(StoreProfileLayout), findsOneWidget);
  expect(find.byType(StoreProfilePanel), findsOneWidget);
}

void _expectStoreProfileStacked(WidgetTester tester) {
  _expectStoreProfileMounted(tester);
  final panel = tester.widget<StoreProfilePanel>(
    find.byType(StoreProfilePanel),
  );
  expect(panel.compact, isTrue);
}

typedef _Open = Future<void> Function(WidgetTester tester);

typedef _Case = ({String name, String role, Object screen, _Open open});

Finder _screenFinder(Object screen) =>
    screen is Finder ? screen : find.byType(screen as Type);

final List<_Case> _detailCases = [
  (
    name: 'admin store detail',
    role: 'Administrator',
    screen: MusicStoreDetailScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Trgovina muzičke opreme');
      unawaited(
        MusicStoreDetailScreen.open(
          tester.element(find.byType(MasterScreen)),
          1,
        ),
      );
      await tester.pumpAndSettle();
    },
  ),
  (
    name: 'admin store form',
    role: 'Administrator',
    screen: MusicStoreFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Trgovina muzičke opreme');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'admin user details',
    role: 'Administrator',
    screen: UserDetailsDialog,
    open: (tester) async {
      await _openMenuEntry(tester, 'Korisnici');
      await _tapFirstCard(tester);
    },
  ),
  (
    name: 'admin user provision form',
    role: 'Administrator',
    screen: UserProvisionFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Korisnici');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'admin event form',
    role: 'Administrator',
    screen: EventFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Događaji');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'admin reference data dialog',
    role: 'Administrator',
    screen: ReferenceDataDialog,
    open: (tester) => _openMenuEntry(tester, 'Referentni podaci'),
  ),
  (
    name: 'admin profile dialog',
    role: 'Administrator',
    screen: ProfileDialog,
    open: (tester) => _openUserMenuItem(tester, 'Profil'),
  ),
  (
    name: 'instructor course detail',
    role: 'Instructor',
    screen: CourseDetailScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Kursevi');
      await _tapFirstCard(tester);
    },
  ),
  (
    name: 'instructor course form',
    role: 'Instructor',
    screen: CourseFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Kursevi');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'instructor catalog detail',
    role: 'Instructor',
    screen: CourseCatalogDetailDialog,
    open: (tester) async {
      await _openMenuEntry(tester, 'Katalog kurseva');
      await _tapFirstCard(tester);
    },
  ),
  (
    name: 'instructor student details',
    role: 'Instructor',
    screen: InstructorStudentDetailsDialog,
    open: (tester) async {
      await _openMenuEntry(tester, 'Moji studenti');
      await _tapFirstCard(tester);
    },
  ),
  (
    name: 'instructor student profile',
    role: 'Instructor',
    screen: StudentProfileDialog,
    open: (tester) async {
      await _openMenuEntry(tester, 'Svi studenti');
      await _tapFirstCard(tester);
    },
  ),
  (
    name: 'employee store form',
    role: 'StoreEmployee',
    screen: ShopStoreFormScreen,
    open: (tester) => _openUserMenuItem(tester, 'Uredi prodavnicu'),
  ),
  (
    name: 'employee instrument form',
    role: 'StoreEmployee',
    screen: InstrumentFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Moja prodavnica');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'employee form',
    role: 'StoreEmployee',
    screen: ShopEmployeeFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Zaposlenici');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'rental detail dialog',
    role: 'StoreEmployee',
    screen: RentalDetailDialog,
    open: (tester) async {
      await _openMenuEntry(tester, kRequestsEntryLabel);
      unawaited(
        RentalDetailDialog.show(tester.element(find.byType(MasterScreen)), 1),
      );
      await tester.pumpAndSettle();
    },
  ),
  (
    name: 'announcement form',
    role: 'StoreEmployee',
    screen: AnnouncementFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Objave');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'announcement detail dialog',
    role: 'StoreEmployee',
    screen: AnnouncementDetailDialog,
    open: (tester) async {
      await _openMenuEntry(tester, 'Objave');
      await _tapRowWithText(tester, 'Naslov 1');
    },
  ),
  (
    name: 'admin change password dialog',
    role: 'Administrator',
    screen: ChangePasswordDialog,
    open: (tester) async {
      await _openUserMenuItem(tester, 'Profil');
      await _tapRowWithText(tester, 'Promijeni lozinku');
    },
  ),
  (
    name: 'admin address form',
    role: 'Administrator',
    screen: AddressFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Referentni podaci');
      await _tapRowWithText(tester, 'Adrese');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'admin city form',
    role: 'Administrator',
    screen: CityFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Referentni podaci');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'admin instrument type form',
    role: 'Administrator',
    screen: InstrumentTypeFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Referentni podaci');
      await _tapRowWithText(tester, 'Tipovi instrumenata');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'instructor lecture form',
    role: 'Instructor',
    screen: LectureFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Kursevi');
      await _tapFirstCard(tester);
      await _tapRowWithText(tester, 'Predavanja');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'instructor assignment form',
    role: 'Instructor',
    screen: AssignmentFormScreen,
    open: (tester) async {
      await _openMenuEntry(tester, 'Kursevi');
      await _tapFirstCard(tester);
      await _tapRowWithText(tester, 'Predavanja');
      await _tapTooltip(tester, 'Zadaci');
      await _tapAdd(tester);
    },
  ),
  (
    name: 'instructor enrollment reject dialog',
    role: 'Instructor',
    screen: ReasonPromptDialog,
    open: (tester) async {
      await _openMenuEntry(tester, kRequestsEntryLabel);
      await _tapScrolledText(tester, 'Odbij');
    },
  ),
  (
    name: 'employee rental reject dialog',
    role: 'StoreEmployee',
    screen: ReasonPromptDialog,
    open: (tester) async {
      await _openMenuEntry(tester, kRequestsEntryLabel);
      await _tapScrolledText(tester, 'Odbij');
    },
  ),
  (
    name: 'employee rental refund dialog',
    role: 'StoreEmployee',
    screen: find.text(
      'Unesite iznos za djelomični povrat. Ostavite prazno za puni povrat.',
    ),
    open: (tester) async {
      await _openMenuEntry(tester, kRequestsEntryLabel);
      unawaited(
        RentalDetailDialog.show(tester.element(find.byType(MasterScreen)), 1),
      );
      await tester.pumpAndSettle();
      await _tapScrolledText(tester, 'Refundiraj');
    },
  ),
];

Future<void> _useMinimumWindow(WidgetTester tester) async {
  tester.view.physicalSize = _testWindowSize;
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

Future<void> _pumpShell(WidgetTester tester, String role) async {
  await _useMinimumWindow(tester);
  await tester.pumpWidget(_buildApp(role));
  await tester.pumpAndSettle();
  expect(find.byType(MasterScreen), findsOneWidget);
}

void main() {
  for (final role in UserRole.values.where((r) => r != UserRole.student)) {
    final entries = [
      for (final entry in kRoleMenuEntries)
        if (!entry.isDialog && entry.allowedRoles.contains(role)) entry,
    ];
    for (final entry in entries) {
      testWidgets(
        'no overflow at the minimum window size: ${role.label} / ${entry.label}',
        (tester) async {
          await _pumpShell(tester, role.label);
          await _openMenuEntry(tester, entry.label);

          final screen = entry.screenBuilder(
            tester.element(find.byType(MasterScreen)),
          );
          expect(find.byType(screen.runtimeType), findsOneWidget);
          if (screen is ShopStoreScreen) {
            _expectStoreProfileStacked(tester);
          }
          expect(tester.takeException(), isNull);
        },
      );
    }
  }

  for (final detail in _detailCases) {
    testWidgets('no overflow at the minimum window size: ${detail.name}', (
      tester,
    ) async {
      await _pumpShell(tester, detail.role);
      await detail.open(tester);

      expect(_screenFinder(detail.screen), findsOneWidget);
      if (detail.screen == MusicStoreDetailScreen) {
        _expectStoreProfileMounted(tester);
      }
      expect(tester.takeException(), isNull);
    });
  }
}
