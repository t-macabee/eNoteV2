import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import 'ranking_provider.dart';

/// S16 — course ranking (02 §5 S16): plain unpaged list (02 D6).
/// The "your row" highlight is applied only when
/// `CourseRankingEntryDto.studentId` matches the profile id (02 §12 item 4);
/// per-row avatars are never requested.
class RankingScreen extends StatefulWidget {
  final int courseId;

  const RankingScreen({super.key, required this.courseId});

  @override
  State<RankingScreen> createState() => _RankingScreenState();
}

class _RankingScreenState extends State<RankingScreen> {
  late final Future<List<CourseRankingEntryDto>> _rankingFuture;

  @override
  void initState() {
    super.initState();
    _rankingFuture = context.read<RankingProvider>().getForCourse(
      widget.courseId,
    );
  }

  @override
  Widget build(BuildContext context) {
    final profileId = context
        .watch<SessionController>()
        .profile
        ?.profile
        .id;
    return Scaffold(
      appBar: AppBar(title: const Text('Rang lista')),
      body: FutureBuilder<List<CourseRankingEntryDto>>(
        future: _rankingFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return AsyncStateView(
              error: snapshot.error,
              onRetry: () => setState(() {
                _rankingFuture = context
                    .read<RankingProvider>()
                    .getForCourse(widget.courseId);
              }),
              child: const SizedBox.shrink(),
            );
          }
          final ranking = snapshot.data ?? [];
          if (ranking.isEmpty) {
            return const Center(
              child: Text('Nema rezultata za pretragu.'),
            );
          }
          return ListView.separated(
            itemCount: ranking.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (context, index) {
              final entry = ranking[index];
              final isMine =
                  profileId != null && entry.studentId == profileId;
              return ListTile(
                tileColor: isMine
                    ? AppTheme.primary.withValues(alpha: 0.15)
                    : null,
                leading: Text(
                  '${entry.rank}',
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
                title: Text(entry.studentName),
                subtitle: Text(
                  entry.averageGrade == null
                      ? 'Bez ocjena'
                      : 'Prosjek ${entry.averageGrade!.toStringAsFixed(2)} · '
                            '${entry.gradedSubmissions} ocijenjenih',
                ),
              );
            },
          );
        },
      ),
    );
  }
}
