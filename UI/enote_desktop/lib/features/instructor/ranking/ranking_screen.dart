import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_empty_state.dart';
import 'ranking_provider.dart';

class RankingView extends StatefulWidget {
  final int courseId;

  const RankingView({super.key, required this.courseId});

  @override
  State<RankingView> createState() => _RankingViewState();
}

class _RankingViewState extends State<RankingView> {
  List<CourseRankingEntryDto> _allItems = [];
  List<CourseRankingEntryDto> _filteredItems = [];
  bool _isLoading = true;
  final _searchController = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
    _searchController.addListener(_onSearchChanged);
  }

  @override
  void dispose() {
    _searchController.removeListener(_onSearchChanged);
    _searchController.dispose();
    super.dispose();
  }

  void _onSearchChanged() {
    final query = _searchController.text.trim().toLowerCase();
    setState(() {
      _searchQuery = query;
      _applyFilter();
    });
  }

  void _applyFilter() {
    if (_searchQuery.isEmpty) {
      _filteredItems = List.from(_allItems);
    } else {
      _filteredItems = _allItems
          .where((e) => e.studentName.toLowerCase().contains(_searchQuery))
          .toList();
    }
  }

  Future<void> _load() async {
    if (mounted) setState(() => _isLoading = true);
    try {
      final provider = context.read<RankingProvider>();
      final items = await provider.getForCourse(widget.courseId);
      if (!mounted) return;
      setState(() {
        _allItems = items;
        _applyFilter();
      });
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_allItems.isEmpty) {
      return const EntityEmptyState(searching: false);
    }
    return Column(
      children: [
        EntityToolbar(
          searchController: _searchController,
          showSearch: true,
          searchHint: 'Pretraži po imenu studenta...',
          showAdd: false,
        ),
        Expanded(
          child: _filteredItems.isEmpty
              ? const EntityEmptyState(searching: true)
              : ListView.separated(
                  itemCount: _filteredItems.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (context, index) {
                    final item = _filteredItems[index];
                    final avg = formatAverageGrade(item.averageGrade);
                    return ListTile(
                      dense: true,
                      leading: Text(
                        '${item.rank}',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      title: Row(
                        children: [
                          UserAvatar(
                            userId: item.studentAppUserId,
                            apiClient: context.read<ApiClient>(),
                            radius: 12,
                            placeholder: () =>
                                InitialsAvatar(text: item.studentName),
                          ),
                          const SizedBox(width: AppSpacing.sm),
                          Expanded(
                            child: Text(
                              item.studentName,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                      subtitle: Text(
                        'Prosjek: $avg · Ocijenjeno: ${item.gradedSubmissions}',
                      ),
                    );
                  },
                ),
        ),
      ],
    );
  }
}
