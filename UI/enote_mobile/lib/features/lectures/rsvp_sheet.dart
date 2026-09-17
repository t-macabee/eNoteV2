import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import 'lecture_provider.dart';

/// S19 — RSVP sheet over S18 (02 §5 S19): no confirm dialog (RSVP is
/// reversible). Server refusals render in the `ErrorBanner` under the button.
class RsvpSheet extends StatefulWidget {
  final int lectureId;
  final bool confirm;
  final VoidCallback onSuccess;

  const RsvpSheet({
    super.key,
    required this.lectureId,
    required this.confirm,
    required this.onSuccess,
  });

  @override
  State<RsvpSheet> createState() => _RsvpSheetState();
}

class _RsvpSheetState extends State<RsvpSheet> {
  String? _error;
  bool _sending = false;

  Future<void> _send() async {
    if (_sending) return;
    setState(() {
      _sending = true;
      _error = null;
    });
    try {
      await context.read<LectureProvider>().rsvp(
        widget.lectureId,
        RsvpRequest(confirm: widget.confirm),
      );
      if (mounted) widget.onSuccess();
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = userMessage(e);
          _sending = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final label = widget.confirm ? 'Dolazim' : 'Ne dolazim';
    return Padding(
      padding: EdgeInsets.only(
        left: 16,
        right: 16,
        top: 16,
        bottom: 16 + MediaQuery.of(context).viewInsets.bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Center(
            child: Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: Theme.of(context).dividerColor,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
          const SizedBox(height: 12),
          Text(label, style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 12),
          FilledButton(
            onPressed: _sending ? null : _send,
            child: _sending
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Text(label),
          ),
          if (_error != null) ...[
            const SizedBox(height: 8),
            ErrorBanner(message: _error!),
          ],
        ],
      ),
    );
  }
}
