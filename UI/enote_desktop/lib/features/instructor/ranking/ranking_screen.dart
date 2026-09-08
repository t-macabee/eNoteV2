import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

class RankingView extends StatefulWidget {
  final int courseId;

  const RankingView({
    super.key,
    required this.courseId,
  });

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
    setState(() => _isLoading = true);
    try {
      final apiClient = context.read<ApiClient>();
      final response = await apiClient.get(
        'instructor/courses/${widget.courseId}/ranking',
      );
      throwIfError(response);
      final list = jsonDecode(response.body) as List;
      final items = list
          .map((e) => CourseRankingEntryDto.fromJson(e as Map<String, dynamic>))
          .toList();
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
      return const SizedBox(
        height: 200,
        child: Center(child: CircularProgressIndicator()),
      );
    }
    if (_allItems.isEmpty) {
      return const SizedBox(
        height: 200,
        child: Center(child: Text('Nema podataka o rangiranju.')),
      );
    }
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Padding(
          padding: const EdgeInsets.all(16),
          child: TextField(
            controller: _searchController,
            decoration: const InputDecoration(
              labelText: 'Pretraži po imenu studenta',
              prefixIcon: Icon(Icons.search),
              border: OutlineInputBorder(),
            ),
          ),
        ),
        Flexible(
          child: _filteredItems.isEmpty
              ? const SizedBox(
                  height: 200,
                  child: Center(child: Text('Nema rezultata za pretragu.')),
                )
              : ListView.separated(
                  shrinkWrap: true,
                  itemCount: _filteredItems.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (context, index) {
                    final item = _filteredItems[index];
                    final avg = item.averageGrade != null
                        ? item.averageGrade!.toStringAsFixed(2)
                        : '-';
                    return ListTile(
                      dense: true,
                      leading: Text(
                        '${item.rank}',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      title: Text(item.studentName),
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
