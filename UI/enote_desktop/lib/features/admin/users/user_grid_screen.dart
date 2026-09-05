import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_grid_screen.dart';
import '../instructor/instructor_provider.dart';
import '../student/student_provider.dart';
import 'store_employee_provider.dart';
import 'user_provision_form_screen.dart';

class _UserListItem {
  final int appUserId;
  final String displayName;
  final UserRole role;
  final String? storeName;
  final DateTime? membershipPaidUntil;

  const _UserListItem({
    required this.appUserId,
    required this.displayName,
    required this.role,
    this.storeName,
    this.membershipPaidUntil,
  });
}

/// Administrator "Users" tab — card grid of Students + Instructors + StoreEmployees,
/// filterable by name and role.
class UserGridScreen extends StatefulWidget {
  const UserGridScreen({super.key});

  @override
  State<UserGridScreen> createState() => _UserGridScreenState();
}

class _UserGridScreenState extends State<UserGridScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<_UserListItem>>();

  /// null = "Svi korisnici" (default) — shows Instruktori + Studenti + StoreEmployee as
  /// labeled sections. Otherwise filters to just that role.
  UserRole? _role;

  static String _formatDisplayName(
    String? firstName,
    String? lastName,
    String? username,
  ) {
    final name = '${firstName ?? ''} ${lastName ?? ''}'.trim();
    if (name.isNotEmpty) return name;
    return username ?? '-';
  }

  Future<void> _openProvisionForm() async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(builder: (_) => const UserProvisionFormScreen()),
    );
    _gridKey.currentState?.refresh();
  }

  Future<void> _renewMembership(
    BuildContext context,
    _UserListItem item,
  ) async {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final initialDate = (item.membershipPaidUntil != null &&
            item.membershipPaidUntil!.isAfter(today))
        ? DateTime(
            item.membershipPaidUntil!.year,
            item.membershipPaidUntil!.month + 1,
            item.membershipPaidUntil!.day,
          )
        : DateTime(now.year, now.month + 1, now.day);

    final pickedDate = await showDatePicker(
      context: context,
      initialDate: initialDate.isBefore(today) ? today : initialDate,
      firstDate: today,
      lastDate: DateTime(today.year + 10),
      helpText: 'Produži članstvo',
      confirmText: 'Sačuvaj',
      cancelText: 'Odustani',
    );

    if (pickedDate == null || !context.mounted) return;

    try {
      final apiClient = context.read<ApiClient>();
      final request = UpdateMembershipRequest(paidUntil: pickedDate);
      final response = await apiClient.put(
        'admin/users/${item.appUserId}/membership',
        body: request.toJson(),
      );
      if (response.statusCode >= 400) {
        throw ApiException(
          ApiErrorMapper.mapError(response.statusCode, response.body),
        );
      }
      _gridKey.currentState?.refresh();
    } catch (e) {
      if (context.mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    }
  }

  void _applyFilters() {
    setState(() {});
    _gridKey.currentState?.refresh(resetPage: true);
  }

  @override
  Widget build(BuildContext context) {
    // Read via `_role` (a field) at call time inside the closures below,
    // rather than snapshotting it into a local here — `setState` doesn't
    // rebuild synchronously, so `_applyFilters` calling
    // `_gridKey.currentState?.refresh()` right after `setState` can still
    // be running against this build's (about-to-be-stale) config. Reading
    // the field directly keeps every closure correct regardless of when it
    // was created.
    return EntityGridScreen<_UserListItem>(
      key: _gridKey,
      config: EntityGridConfig<_UserListItem>(
        searchHint: 'Pretraži po imenu...',
        placeholderIcon: Icons.person_outline,
        titleOf: (item) => item.displayName,
        subtitleOf: (item) => switch (item.role) {
          UserRole.student => 'Produži članstvo',
          UserRole.storeEmployee =>
            item.storeName != null && item.storeName!.isNotEmpty
                ? item.storeName
                : null,
          _ => null,
        },
        onTap: (context, item) async {
          if (item.role != UserRole.student) return;
          await _renewMembership(context, item);
        },
        onDelete: (context, item) async {
          final apiClient = context.read<ApiClient>();
          final response = await apiClient.delete(
            'admin/users/${item.appUserId}',
          );
          if (response.statusCode >= 400) {
            throw ApiException(
              ApiErrorMapper.mapError(response.statusCode, response.body),
            );
          }
          return true;
        },
        groupKeyOf: _role == null
            ? (item) => switch (item.role) {
                UserRole.instructor => 'Instruktori',
                UserRole.student => 'Studenti',
                UserRole.storeEmployee => 'StoreEmployee',
                _ => item.role.label,
              }
            : null,
        filterBar: SizedBox(
          width: 220,
          child: DropdownButtonFormField<UserRole?>(
            isExpanded: true,
            initialValue: _role,
            decoration: const InputDecoration(labelText: 'Uloga'),
            items: const [
              DropdownMenuItem(value: null, child: Text('Svi korisnici')),
              DropdownMenuItem(
                value: UserRole.instructor,
                child: Text('Instruktor'),
              ),
              DropdownMenuItem(value: UserRole.student, child: Text('Student')),
              DropdownMenuItem(
                value: UserRole.storeEmployee,
                child: Text('StoreEmployee'),
              ),
            ],
            onChanged: (role) {
              _role = role;
              _applyFilters();
            },
          ),
        ),
        fetcher: (page, pageSize, search) async {
          final query = {
            'page': page,
            'pageSize': pageSize,
            'includeTotalCount': true,
            if (search.isNotEmpty) 'name': search,
          };

          if (_role == UserRole.instructor) {
            final result =
                await context.read<InstructorProvider>().search(query);
            return PagedResult<_UserListItem>(
              items: result.items
                  .map(
                    (i) => _UserListItem(
                      appUserId: i.appUserId,
                      displayName: _formatDisplayName(
                        i.firstName,
                        i.lastName,
                        i.username,
                      ),
                      role: UserRole.instructor,
                    ),
                  )
                  .toList(),
              page: result.page,
              pageSize: result.pageSize,
              totalCount: result.totalCount,
            );
          }

          if (_role == UserRole.student) {
            final result = await context.read<StudentProvider>().search(query);
            return PagedResult<_UserListItem>(
              items: result.items
                  .map(
                    (s) => _UserListItem(
                      appUserId: s.appUserId,
                      displayName: _formatDisplayName(
                        s.firstName,
                        s.lastName,
                        s.username,
                      ),
                      role: UserRole.student,
                      membershipPaidUntil: s.membershipPaidUntil,
                    ),
                  )
                  .toList(),
              page: result.page,
              pageSize: result.pageSize,
              totalCount: result.totalCount,
            );
          }

          if (_role == UserRole.storeEmployee) {
            final result =
                await context.read<StoreEmployeeProvider>().search(query);
            return PagedResult<_UserListItem>(
              items: result.items
                  .map(
                    (e) => _UserListItem(
                      appUserId: e.appUserId,
                      displayName: _formatDisplayName(
                        e.firstName,
                        e.lastName,
                        e.username,
                      ),
                      role: UserRole.storeEmployee,
                      storeName: e.storeName,
                    ),
                  )
                  .toList(),
              page: result.page,
              pageSize: result.pageSize,
              totalCount: result.totalCount,
            );
          }

          // _role == null: fetch instructors, students, and store employees concurrently
          final instructorFuture =
              context.read<InstructorProvider>().search(query);
          final studentFuture = context.read<StudentProvider>().search(query);
          final employeeFuture =
              context.read<StoreEmployeeProvider>().search(query);
          final results = await Future.wait([
            instructorFuture,
            studentFuture,
            employeeFuture,
          ]);
          final instructorResult = results[0] as PagedResult<InstructorDto>;
          final studentResult = results[1] as PagedResult<StudentDto>;
          final employeeResult = results[2] as PagedResult<ShopEmployeeDto>;

          final instructorItems = instructorResult.items.map(
            (i) => _UserListItem(
              appUserId: i.appUserId,
              displayName: _formatDisplayName(
                i.firstName,
                i.lastName,
                i.username,
              ),
              role: UserRole.instructor,
            ),
          );
          final studentItems = studentResult.items.map(
            (s) => _UserListItem(
              appUserId: s.appUserId,
              displayName: _formatDisplayName(
                s.firstName,
                s.lastName,
                s.username,
              ),
              role: UserRole.student,
              membershipPaidUntil: s.membershipPaidUntil,
            ),
          );
          final employeeItems = employeeResult.items.map(
            (e) => _UserListItem(
              appUserId: e.appUserId,
              displayName: _formatDisplayName(
                e.firstName,
                e.lastName,
                e.username,
              ),
              role: UserRole.storeEmployee,
              storeName: e.storeName,
            ),
          );

          final totalCount = (instructorResult.totalCount != null ||
                  studentResult.totalCount != null ||
                  employeeResult.totalCount != null)
              ? (instructorResult.totalCount ?? 0) +
                  (studentResult.totalCount ?? 0) +
                  (employeeResult.totalCount ?? 0)
              : null;

          return PagedResult<_UserListItem>(
            items: [...instructorItems, ...studentItems, ...employeeItems],
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
          );
        },
        onAdd: () => _openProvisionForm(),
        addLabel: 'Kreiraj korisnika',
      ),
    );
  }
}
