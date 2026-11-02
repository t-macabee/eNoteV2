import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/detail_actions.dart';
import '../../widgets/detail_header.dart';
import '../../widgets/detail_section.dart';
import '../../widgets/labeled_value.dart';
import '../../widgets/submit_button.dart';
import 'lecture_labels.dart';
import 'lecture_provider.dart';

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
  bool? _pendingConfirm;

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

  Future<void> _rsvp(bool confirm) async {
    if (_pendingConfirm != null) return;
    setState(() => _pendingConfirm = confirm);
    try {
      await context.read<LectureProvider>().rsvp(
        widget.lectureId,
        RsvpRequest(confirm: confirm),
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            confirm ? 'Prisustvo je potvrđeno.' : 'Prisustvo je odbijeno.',
          ),
        ),
      );
      await _load();
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
    } finally {
      if (mounted) setState(() => _pendingConfirm = null);
    }
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
        child: lecture == null ? const SizedBox.shrink() : _body(lecture),
      ),
    );
  }

  Widget _body(LectureDto lecture) {
    final closedMessage = _closedBannerMessage(lecture);
    final blockedReason = _blockedReason(lecture);
    final reason = closedMessage ?? blockedReason;
    final rsvpHidden = _isClosed(lecture);
    final attendance = lecture.myAttendanceStatus;
    final isBusy = _pendingConfirm != null;
    final canAct = !rsvpHidden && blockedReason == null && !isBusy;
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.only(bottom: AppSpacing.lg),
        children: [
          DetailHeader(
            title: lecture.name,
            subtitle: formatDateTime(lecture.lectureTime),
            chips: [
              StatusChip(
                label: lectureStatusLabel(lecture.lectureStatus),
                tone: lectureStatusTone(lecture.lectureStatus),
              ),
              ...lectureStateChips(lecture),
            ],
          ),
          DetailSection(
            title: 'Detalji',
            child: Column(
              children: [
                LabeledValue(
                  label: 'Trajanje',
                  value: '${lecture.duration} min',
                ),
                LabeledValue(label: 'Lokacija', value: lecture.location),
                LabeledValue(
                  label: 'Tip',
                  value: lectureTypeLabel(lecture.lectureType),
                ),
                if (lecture.cancellationReason != null)
                  LabeledValue(
                    label: 'Razlog otkazivanja',
                    value: lecture.cancellationReason!,
                  ),
                LabeledValue(
                  label: 'Prijavljeno',
                  value:
                      '${lecture.attendeeCount}/${lecture.capacity?.toString() ?? '∞'}',
                ),
              ],
            ),
          ),
          DetailActions(
            banner: reason == null
                ? null
                : StatusBanner(
                    icon: Icons.info_outline,
                    message: reason,
                    tone: StatusTone.neutral,
                  ),
            actions: [
              if (!rsvpHidden && attendance != AttendanceStatus.present)
                SubmitButton(
                  label: 'Dolazim',
                  onPressed: canAct ? () => _rsvp(true) : null,
                  isBusy: _pendingConfirm == true,
                ),
              if (!rsvpHidden && attendance != AttendanceStatus.absent)
                OutlinedButton(
                  onPressed: canAct ? () => _rsvp(false) : null,
                  child: _pendingConfirm == false
                      ? const SizedBox.square(
                          dimension: AppSize.spinnerMd,
                          child: CircularProgressIndicator(
                            strokeWidth: AppSize.spinnerStroke,
                          ),
                        )
                      : const Text('Ne dolazim'),
                ),
              OutlinedButton(
                onPressed: () => Navigator.of(context).pushNamed(
                  AppRoutes.lectureNotes,
                  arguments: LectureNotesArgs(lecture.id),
                ),
                child: const Text('Bilješke'),
              ),
            ],
          ),
        ],
      ),
    );
  }

  bool _isClosed(LectureDto lecture) {
    if (lecture.isCancelled ||
        lecture.lectureStatus == LectureStatus.cancelled) {
      return true;
    }
    if (lecture.lectureStatus == LectureStatus.held) {
      return true;
    }
    return DateTime.now().isAfter(lecture.lectureTime);
  }

  String? _closedBannerMessage(LectureDto lecture) {
    if (lecture.isCancelled ||
        lecture.lectureStatus == LectureStatus.cancelled) {
      return null;
    }
    if (lecture.lectureStatus == LectureStatus.held) {
      return null;
    }
    if (DateTime.now().isAfter(lecture.lectureTime)) {
      return 'Prijave su zatvorene jer je predavanje počelo.';
    }
    return null;
  }

  String? _blockedReason(LectureDto lecture) {
    final capacity = lecture.capacity;
    final confirmed = lecture.myAttendanceStatus == AttendanceStatus.present;
    if (capacity != null && lecture.attendeeCount >= capacity && !confirmed) {
      return 'Predavanje je popunjeno.';
    }
    return null;
  }
}
