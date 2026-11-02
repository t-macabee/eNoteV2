import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../session/session_controller.dart';
import '../../widgets/async_state_view.dart';
import 'ranking_provider.dart';

class RankingScreen extends StatefulWidget {
  final int courseId;

  const RankingScreen({super.key, required this.courseId});

  @override
  State<RankingScreen> createState() => _RankingScreenState();
}

class _RankingScreenState extends State<RankingScreen> {
  List<CourseRankingEntryDto>? _ranking;
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final ranking = await context.read<RankingProvider>().getForCourse(
        widget.courseId,
      );
      if (mounted) {
        setState(() {
          _ranking = ranking;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e;
          _loading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final profileId = context.watch<SessionController>().profile?.profile.id;
    final ranking = _ranking;
    return Scaffold(
      appBar: AppBar(title: const Text('Rang lista – 15 najboljih')),
      body: AsyncStateView(
        isLoading: _loading && ranking == null,
        error: ranking == null ? _error : null,
        isEmpty: ranking != null && ranking.isEmpty,
        emptyMessage: 'Još nema rangiranih studenata.',
        onRetry: _load,
        child: ranking == null
            ? const SizedBox.shrink()
            : RefreshIndicator(
                onRefresh: _load,
                child: ListView.separated(
                  physics: const AlwaysScrollableScrollPhysics(),
                  itemCount: ranking.length,
                  separatorBuilder: (_, index) =>
                      ranking[index + 1].rank > ranking[index].rank + 1
                      ? const Padding(
                          padding: EdgeInsets.symmetric(
                            vertical: AppSpacing.xs,
                          ),
                          child: Center(child: Text('…')),
                        )
                      : const Divider(height: 1),
                  itemBuilder: (context, index) {
                    final entry = ranking[index];
                    final isMine =
                        profileId != null && entry.studentId == profileId;
                    return ListTile(
                      tileColor: isMine ? StatusTone.accent.background : null,
                      leading: Text(
                        '${entry.rank}',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      title: Text(entry.studentName),
                      subtitle: Text(
                        entry.averageGrade == null
                            ? 'Bez ocjena'
                            : 'Prosjek ${formatAverageGrade(entry.averageGrade)} · '
                                  '${entry.gradedSubmissions} ocijenjenih',
                      ),
                    );
                  },
                ),
              ),
      ),
    );
  }
}
