import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../instructor/instructor_provider.dart';
import 'user_provision_form_screen.dart';

const String _studentGapMessage =
    'Prikaz studenata još nije dostupan — nedostaje admin endpoint za listu '
    'studenata.';

/// Administrator "Users" tab — card grid of Students + Instructors,
/// filterable by name and role.
///
/// Only the Instructor side is backed by a real endpoint today
/// (`AdminInstructorController.GetPaged`). There is no admin-scoped Student
/// list endpoint (`AdminUsersController` only has `GetById`/`Provision`/
/// `UpdateMembership`) — see the Admin IA rework prompt, point 2. Selecting
/// "Student" (or the "Svi korisnici" default, which shows both sections)
/// surfaces that gap instead of silently showing wrong data.
class UserGridScreen extends StatefulWidget {
  const UserGridScreen({super.key});

  @override
  State<UserGridScreen> createState() => _UserGridScreenState();
}

class _UserGridScreenState extends State<UserGridScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<InstructorDto>>();

  /// null = "Svi korisnici" (default) — shows Instruktori + Studenti as two
  /// labeled sections. Otherwise filters to just that role.
  UserRole? _role;

  static String _displayName(InstructorDto item) {
    final name = '${item.firstName ?? ''} ${item.lastName ?? ''}'.trim();
    if (name.isNotEmpty) return name;
    return item.username ?? '-';
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
    return EntityGridScreen<InstructorDto>(
      key: _gridKey,
      config: EntityGridConfig<InstructorDto>(
        title: 'Korisnici',
        searchHint: 'Pretraži po imenu...',
        placeholderIcon: Icons.person_outline,
        titleOf: _displayName,
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
        emptyMessage: _role == UserRole.student
            ? _studentGapMessage
            : 'Nema podataka.',
        aboveGrid: _role == null
            ? const EntitySectionLabel('Instruktori')
            : null,
        belowGrid: _role == null
            ? const Padding(
                padding: EdgeInsets.only(top: 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    EntitySectionLabel('Studenti'),
                    SizedBox(height: 12),
                    Text(
                      _studentGapMessage,
                      style: TextStyle(color: AppTheme.textSecondary),
                    ),
                  ],
                ),
              )
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
          if (_role == UserRole.student) {
            return PagedResult<InstructorDto>(
              items: const [],
              page: page,
              pageSize: pageSize,
              totalCount: 0,
            );
          }
          // null (Svi korisnici) and UserRole.instructor both show
          // Instruktori — it's the only role with real data.
          return context.read<InstructorProvider>().search({
            'page': page,
            'pageSize': pageSize,
            'includeTotalCount': true,
            if (search.isNotEmpty) 'name': search,
          });
        },
        onAdd: () => _openProvisionForm(),
        addLabel: 'Kreiraj korisnika',
      ),
    );
  }
}
