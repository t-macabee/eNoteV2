import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/pdf_report_button.dart';
import 'lecture_provider.dart';

String _attendanceLabel(AttendanceStatus status) => switch (status) {
      AttendanceStatus.pending => 'Na čekanju',
      AttendanceStatus.present => 'Prisutan',
      AttendanceStatus.absent => 'Odsutan',
    };

Color _attendanceColor(AttendanceStatus status) => switch (status) {
      AttendanceStatus.pending => Colors.orange,
      AttendanceStatus.present => Colors.green,
      AttendanceStatus.absent => Colors.red,
    };

class LectureAttendanceScreen extends StatefulWidget {
  final int lectureId;
  final String lectureName;

  const LectureAttendanceScreen({
    super.key,
    required this.lectureId,
    required this.lectureName,
  });

  @override
  State<LectureAttendanceScreen> createState() => _LectureAttendanceScreenState();
}

class _LectureAttendanceScreenState extends State<LectureAttendanceScreen> {
  List<AttendanceDto> _items = [];
  bool _isLoading = true;
  int _currentPage = 1;
  final int _pageSize = 20;
  int? _totalCount;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    setState(() => _isLoading = true);
    try {
      final provider = context.read<LectureProvider>();
      final result = await provider.getAttendance(
        widget.lectureId,
        params: {
          'page': _currentPage,
          'pageSize': _pageSize,
          'includeTotalCount': true,
        },
      );
      setState(() {
        _items = result.items;
        _totalCount = result.totalCount;
      });
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _markAttendance(AttendanceDto item, AttendanceStatus newStatus) async {
    try {
      final provider = context.read<LectureProvider>();
      await provider.markAttendance(
        widget.lectureId,
        MarkAttendanceRequest(
          studentId: item.studentId,
          attendanceStatus: newStatus,
        ),
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Prisustvo ažurirano: ${_attendanceLabel(newStatus)}')),
      );
      _load();
    } catch (e) {
      if (!mounted) return;
      ErrorBanner.show(context, message: userMessage(e));
    }
  }

  Future<void> _showStatusPicker(AttendanceDto item) async {
    final selected = await showDialog<AttendanceStatus>(
      context: context,
      builder: (context) => SimpleDialog(
        title: Text('Prisustvo — ${item.studentName}'),
        children: [
          for (final status in [AttendanceStatus.present, AttendanceStatus.absent])
            SimpleDialogOption(
              onPressed: () => Navigator.pop(context, status),
              child: Row(
                children: [
                  Icon(
                    status == AttendanceStatus.present ? Icons.check_circle : Icons.cancel,
                    color: _attendanceColor(status),
                    size: 18,
                  ),
                  const SizedBox(width: 8),
                  Text(_attendanceLabel(status)),
                ],
              ),
            ),
        ],
      ),
    );
    if (selected != null) {
      await _markAttendance(item, selected);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Prisustvo — ${widget.lectureName}'),
        actions: [
          PdfReportButton(
            label: 'Izvještaj',
            fileName: 'lecture-${widget.lectureId}-attendance.pdf',
            endpoint: 'instructor/lectures/${widget.lectureId}/attendance/report',
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _items.isEmpty
                    ? const Center(child: Text('Nema podataka o prisustvu.'))
                    : SingleChildScrollView(
                        child: DataTable(
                          columns: const [
                            DataColumn(label: Text('Student')),
                            DataColumn(label: Text('Status')),
                            DataColumn(label: Text('Akcija')),
                          ],
                          rows: _items.map((item) {
                            return DataRow(
                              cells: [
                                DataCell(Text(item.studentName)),
                                DataCell(
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                    decoration: BoxDecoration(
                                      color: _attendanceColor(item.attendanceStatus).withValues(alpha: 0.15),
                                      borderRadius: BorderRadius.circular(4),
                                    ),
                                    child: Text(
                                      _attendanceLabel(item.attendanceStatus),
                                      style: TextStyle(
                                        color: _attendanceColor(item.attendanceStatus),
                                        fontWeight: FontWeight.w500,
                                      ),
                                    ),
                                  ),
                                ),
                                DataCell(
                                  IconButton(
                                    icon: const Icon(Icons.edit, size: 18),
                                    tooltip: 'Promijeni status',
                                    onPressed: () => _showStatusPicker(item),
                                  ),
                                ),
                              ],
                            );
                          }).toList(),
                        ),
                      ),
          ),
          _buildPagination(),
        ],
      ),
    );
  }

  Widget _buildPagination() {
    if (_totalCount == null) return const SizedBox.shrink();
    final totalPages = (_totalCount! / _pageSize).ceil();
    final hasPrev = _currentPage > 1;
    final hasNext = _currentPage < totalPages;
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Text('Stranica $_currentPage od $totalPages'),
          const SizedBox(width: 16),
          Text('Ukupno: $_totalCount'),
          const SizedBox(width: 16),
          TextButton.icon(
            onPressed: hasPrev
                ? () {
                    setState(() => _currentPage--);
                    _load();
                  }
                : null,
            icon: const Icon(Icons.chevron_left),
            label: const Text('Prethodna'),
          ),
          TextButton.icon(
            onPressed: hasNext
                ? () {
                    setState(() => _currentPage++);
                    _load();
                  }
                : null,
            icon: const Icon(Icons.chevron_right),
            label: const Text('Sledeća'),
          ),
        ],
      ),
    );
  }
}
