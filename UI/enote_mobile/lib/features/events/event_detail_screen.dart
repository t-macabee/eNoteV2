import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/detail_header.dart';
import '../../widgets/detail_section.dart';
import '../../widgets/labeled_value.dart';

class EventDetailScreen extends StatelessWidget {
  final EventDto event;

  const EventDetailScreen({super.key, required this.event});

  @override
  Widget build(BuildContext context) {
    final endsAt = event.endsAt;
    final address = [
      event.addressStreet,
      event.addressCity,
    ].whereType<String>().where((s) => s.isNotEmpty).join(', ');
    final timeRange = endsAt == null
        ? formatDateTime(event.startsAt)
        : '${formatDateTime(event.startsAt)} – ${formatDateTime(endsAt)}';
    return Scaffold(
      appBar: AppBar(title: const Text('Događaj')),
      body: ListView(
        padding: const EdgeInsets.only(bottom: AppSpacing.lg),
        children: [
          DetailHeader(title: event.title, subtitle: timeRange),
          DetailSection(
            title: 'Detalji',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                LabeledValue(
                  label: 'Adresa',
                  value: address.isEmpty ? '—' : address,
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  event.description,
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
