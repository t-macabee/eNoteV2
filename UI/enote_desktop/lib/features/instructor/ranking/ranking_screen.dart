import 'dart:convert';
import 'dart:typed_data';

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
  List<CourseRankingEntryDto> _items = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    setState(() => _isLoading = true);
    try {
      final apiClient = context.read<ApiClient>();
      final response = await apiClient.get(
        'instructor/courses/${widget.courseId}/ranking',
      );
      if (response.statusCode >= 400) {
        throw ApiException(
          ApiErrorMapper.mapError(response.statusCode, response.body),
        );
      }
      final list = jsonDecode(response.body) as List;
      setState(() {
        _items = list
            .map((e) => CourseRankingEntryDto.fromJson(e as Map<String, dynamic>))
            .toList();
      });
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: e.toString());
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
            fetchPdf: () async {
              final apiClient = context.read<ApiClient>();
              final response = await apiClient.get(
                'instructor/courses/${widget.courseId}/ranking/report',
              );
              if (response.statusCode >= 400) {
                throw ApiException(
                  ApiErrorMapper.mapError(response.statusCode, response.body),
                );
              }
              return Uint8List.fromList(response.bodyBytes);
            },
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _items.isEmpty
              ? const Center(child: Text('Nema podataka o rangiranju.'))
              : SingleChildScrollView(
                  child: DataTable(
                    columns: const [
                      DataColumn(label: Text('Rang')),
                      DataColumn(label: Text('Student')),
                      DataColumn(label: Text('Prosjek')),
                      DataColumn(label: Text('Broj ocijenjenih predaja')),
                    ],
                    rows: _items.map((item) {
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
    );
  }
}
