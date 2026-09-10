import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/blocked_reason_banner.dart';
import '../../widgets/labeled_value.dart';
import 'lecture_labels.dart';
import 'lecture_provider.dart';
import 'rsvp_sheet.dart';

/// S18 — lecture detail (02 §5 S18) with the RSVP section (S19 sheet).
class LectureDetailScreen extends StatefulWidget {
  final int lectureId;

  const LectureDetailScreen({super.key, required this.lectureId});

  @override
  State<LectureDetailScreen> createState() => _LectureDetailScreenState();
}

class _LectureDetailScreenState extends State<LectureDetailScreen> {
  LectureDto? _lecture;
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
      final lecture = await context.read<LectureProvider>().getById(
        widget.lectureId,
      );
      if (mounted) {
        setState(() {
          _lecture = lecture;
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

  void _openRsvp(bool confirm) {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (sheetContext) => RsvpSheet(
        lectureId: widget.lectureId,
        confirm: confirm,
        onSuccess: () {
          Navigator.of(sheetContext).pop();
          _load();
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                confirm ? 'Prisustvo je potvrđeno.' : 'Prisustvo je odbijeno.',
              ),
            ),
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final lecture = _lecture;
    return Scaffold(
      appBar: AppBar(title: const Text('Predavanje')),
      body: AsyncStateView(
        isLoading: _loading && lecture == null,
        error: lecture == null ? _error : null,
        onRetry: _load,
        child: lecture == null
            ? const SizedBox.shrink()
            : _body(lecture),
      ),
    );
  }

  Widget _body(LectureDto lecture) {
    final blockedReason = _blockedReason(lecture);
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            lecture.name,
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 12),
          LabeledValue(
            label: 'Vrijeme',
            value: formatDateTime(lecture.lectureTime),
          ),
          LabeledValue(
            label: 'Trajanje',
            value: '${lecture.duration} min',
          ),
          LabeledValue(label: 'Lokacija', value: lecture.location),
          LabeledValue(
            label: 'Tip',
            value: lectureTypeLabel(lecture.lectureType),
          ),
          LabeledValue(
            label: 'Status',
            value: lectureStatusLabel(lecture.lectureStatus),
          ),
          LabeledValue(
            label: 'Prijavljeno',
            value:
                '${lecture.attendeeCount}/${lecture.capacity?.toString() ?? '∞'}',
          ),
          const SizedBox(height: 16),
          Text(
            rsvpStateLabel(lecture.myAttendanceStatus),
            style: Theme.of(context).textTheme.titleMedium,
          ),
          if (blockedReason != null) ...[
            const SizedBox(height: 8),
            BlockedReasonBanner(
              icon: Icons.info_outline,
              reason: blockedReason,
            ),
          ],
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: FilledButton(
                  onPressed: blockedReason == null
                      ? () => _openRsvp(true)
                      : null,
                  child: const Text('Dolazim'),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: OutlinedButton(
                  onPressed: blockedReason == null
                      ? () => _openRsvp(false)
                      : null,
                  child: const Text('Ne dolazim'),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          OutlinedButton(
            onPressed: () => Navigator.of(context).pushNamed(
              AppRouter.lectureNotes,
              arguments: LectureNotesArgs(lecture.id),
            ),
            child: const Text('Bilješke'),
          ),
        ],
      ),
    );
  }

  /// Client-derived disabled reason (03 P3): cancelled, held, or full while
  /// the student has not confirmed yet.
  String? _blockedReason(LectureDto lecture) {
    if (lecture.isCancelled ||
        lecture.lectureStatus == LectureStatus.cancelled) {
      return 'Predavanje je otkazano.';
    }
    if (lecture.lectureStatus == LectureStatus.held) {
      return 'Predavanje je održano.';
    }
    final capacity = lecture.capacity;
    final confirmed =
        lecture.myAttendanceStatus == AttendanceStatus.present;
    if (capacity != null && lecture.attendeeCount >= capacity && !confirmed) {
      return 'Predavanje je popunjeno.';
    }
    return null;
  }
}
