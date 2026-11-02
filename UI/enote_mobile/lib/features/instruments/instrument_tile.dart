import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import 'instrument_labels.dart';

class InstrumentTile extends StatelessWidget {
  final InstrumentDto instrument;
  final VoidCallback onTap;

  const InstrumentTile({
    super.key,
    required this.instrument,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      onTap: onTap,
      leading: ImageThumbnail(
        imageUrl: instrument.imagePath,
        apiClient: context.read<ApiClient>(),
        size: AppSize.listThumbnail,
        borderRadius: AppRadius.md,
      ),
      title: Text('${instrument.manufacturer} ${instrument.model}'),
      subtitle: Text('${instrument.instrumentType} · ${instrument.musicStore}'),
      trailing: instrumentAvailabilityChip(instrument.isAvailable),
    );
  }
}
