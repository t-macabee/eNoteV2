import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_list_screen.dart';
import '../announcement/announcement_list_screen.dart';
import '../announcement/announcement_provider.dart';
import '../lecture/lecture_list_screen.dart';
import '../ranking/ranking_screen.dart';
import 'course_form_screen.dart';
import 'course_provider.dart';



class CourseListScreen extends StatefulWidget {
  const CourseListScreen({super.key});

  @override
  State<CourseListScreen> createState() => _CourseListScreenState();
}

class _CourseListScreenState extends State<CourseListScreen> {
  final _listKey = GlobalKey<EntityListScreenState<CourseDto>>();

  Future<void> _openForm([CourseDto? existing]) async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => CourseFormScreen(
        existing: existing,
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _listKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return EntityListScreen<CourseDto>(
      key: _listKey,
      config: EntityListConfig<CourseDto>(
        columns: [
          ColumnSpec<CourseDto>(
            label: 'Naziv',
            value: (item) => item.name,
          ),
          ColumnSpec<CourseDto>(
            label: 'Cijena',
            value: (item) => item.price.toStringAsFixed(2),
          ),
          ColumnSpec<CourseDto>(
            label: 'Objavljen',
            value: (item) => item.isPublished ? 'Da' : 'Ne',
          ),
          ColumnSpec<CourseDto>(
            label: 'Broj upisanih',
            value: (item) => item.enrolledCount,
          ),
          ColumnSpec<CourseDto>(
            label: 'Datum početka',
            value: (item) => formatDateNullable(item.startDate),
          ),
        ],
        fetcher: (page, pageSize, search) =>
            context.read<CourseProvider>().search({
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
          if (search.isNotEmpty) 'name': search,
        }),
        showDeleteConfirmation: false,
        onAdd: () => _openForm(),
        onEdit: (context, item) => _openForm(item),
        // Custom confirm message: delete also deactivates lectures.
        // EntityListScreen's built-in dialog is generic, so we disable it
        // and handle confirmation here to show the specific soft-delete text.
        extraActions: (context, item) => [
          IconButton(
            icon: const Icon(Icons.event_note, size: 18),
            tooltip: 'Predavanja',
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute<void>(
                  builder: (_) => LectureListScreen(
                    courseId: item.id,
                    courseName: item.name,
                  ),
                ),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.campaign, size: 18),
            tooltip: 'Objave',
            onPressed: () {
              final apiClient = context.read<ApiClient>();
              Navigator.of(context).push(
                MaterialPageRoute<void>(
                  builder: (_) => ChangeNotifierProvider<AnnouncementProvider>(
                    create: (_) => AnnouncementProvider(
                      apiClient: apiClient,
                      courseId: item.id,
                    ),
                    child: AnnouncementListScreen(
                      courseId: item.id,
                      courseName: item.name,
                    ),
                  ),
                ),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.leaderboard, size: 18),
            tooltip: 'Rangiranje',
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute<void>(
                  builder: (_) => RankingScreen(
                    courseId: item.id,
                    courseName: item.name,
                  ),
                ),
              );
            },
          ),
        ],
        onDelete: (context, item) async {
          final provider = context.read<CourseProvider>();
          final confirmed = await confirmDialog(
            context: context,
            title: 'Potvrdite brisanje',
            message:
                'Da li ste sigurni da želite da obrišete ovaj kurs? Brisanjem kursa deaktiviraće se i njegova predavanja.',
          );
          if (confirmed != true) return false;
          await provider.remove(item.id);
          return true;
        },
      ),
    );
  }
}
