import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/pdf_report_button.dart';

class RankingScreen extends StatefulWidget {
  final int courseId;
  final String courseName;

  const RankingScreen({
    super.key,
    required this.courseId,
    required this.courseName,
  });

  @override
  State<RankingScreen> createState() => _RankingScreenState();
}

class _RankingScreenState extends State<RankingScreen> {
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
    return Scaffold(
      appBar: AppBar(
        title: Text('Rangiranje — ${widget.courseName}'),
        actions: [
          PdfReportButton(
            label: 'Izvještaj',
            fileName: 'course-${widget.courseId}-ranking.pdf',
            endpoint: 'instructor/courses/${widget.courseId}/ranking/report',
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _allItems.isEmpty
              ? const Center(child: Text('Nema podataka o rangiranju.'))
              : Column(
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
                    Expanded(
                      child: _filteredItems.isEmpty
                          ? const Center(child: Text('Nema rezultata za pretragu.'))
                          : SingleChildScrollView(
                              child: DataTable(
                                columns: const [
                                  DataColumn(label: Text('Rang')),
                                  DataColumn(label: Text('Student')),
                                  DataColumn(label: Text('Prosjek')),
                                  DataColumn(label: Text('Broj ocijenjenih predaja')),
                                ],
                                rows: _filteredItems.map((item) {
                      return DataRow(
                        cells: [
                          DataCell(Text(item.rank.toString())),
                          DataCell(Text(item.studentName)),
                          DataCell(
                            Text(
                              item.averageGrade != null
                                  ? item.averageGrade!.toStringAsFixed(2)
                                  : '-',
                            ),
                          ),
                          DataCell(Text(item.gradedSubmissions.toString())),
                        ],
                      );
                    }).toList(),
                              ),
                            ),
                    ),
                  ],
                ),
    );
  }
}
