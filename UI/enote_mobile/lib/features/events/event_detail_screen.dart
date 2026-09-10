import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/labeled_value.dart';

/// S28 — event detail (02 §5 S28): the passed object is already loaded, so no
/// request is issued. `addressId` is never shown.
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
    return Scaffold(
      appBar: AppBar(title: const Text('Događaj')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            event.title,
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 12),
          LabeledValue(
            label: 'Vrijeme',
            value: endsAt == null
                ? formatDateTime(event.startsAt)
                : '${formatDateTime(event.startsAt)} – ${formatDateTime(endsAt)}',
          ),
          LabeledValue(
            label: 'Adresa',
            value: address.isEmpty ? '—' : address,
          ),
          const SizedBox(height: 8),
          Text(
            event.description,
            style: Theme.of(context).textTheme.bodyMedium,
          ),
        ],
      ),
    );
  }
}
