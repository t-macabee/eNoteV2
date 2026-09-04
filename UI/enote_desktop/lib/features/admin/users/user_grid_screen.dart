import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_grid_screen.dart';
import '../instructor/instructor_provider.dart';
import '../student/student_provider.dart';
import 'user_provision_form_screen.dart';

class _UserListItem {
  final int appUserId;
  final String displayName;
  final UserRole role;

  const _UserListItem({
    required this.appUserId,
    required this.displayName,
    required this.role,
  });
}

/// Administrator "Users" tab — card grid of Students + Instructors,
/// filterable by name and role.
class UserGridScreen extends StatefulWidget {
  const UserGridScreen({super.key});

  @override
  State<UserGridScreen> createState() => _UserGridScreenState();
}

class _UserGridScreenState extends State<UserGridScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<_UserListItem>>();

  /// null = "Svi korisnici" (default) — shows Instruktori + Studenti as two
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
            ? (item) => item.role == UserRole.instructor
                ? 'Instruktori'
                : 'Studenti'
            : null,
        filterBar: SizedBox(
          width: 220,
          child: DropdownButtonFormField<UserRole?>(
            initialValue: _role,
            decoration: const InputDecoration(labelText: 'Uloga'),
            items: const [
              DropdownMenuItem(value: null, child: Text('Svi korisnici')),
              DropdownMenuItem(
                value: UserRole.instructor,
                child: Text('Instruktor'),
              ),
              DropdownMenuItem(value: UserRole.student, child: Text('Student')),
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
                    ),
                  )
                  .toList(),
              page: result.page,
              pageSize: result.pageSize,
              totalCount: result.totalCount,
            );
          }

          // _role == null: fetch both instructors and students concurrently
          final instructorFuture =
              context.read<InstructorProvider>().search(query);
          final studentFuture = context.read<StudentProvider>().search(query);
          final results = await Future.wait([instructorFuture, studentFuture]);
          final instructorResult = results[0] as PagedResult<InstructorDto>;
          final studentResult = results[1] as PagedResult<StudentDto>;

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
            ),
          );

          final totalCount = (instructorResult.totalCount != null ||
                  studentResult.totalCount != null)
              ? (instructorResult.totalCount ?? 0) +
                  (studentResult.totalCount ?? 0)
              : null;

          return PagedResult<_UserListItem>(
            items: [...instructorItems, ...studentItems],
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
