import 'package:enote_core/enote_core.dart';

StatusChip instrumentAvailabilityChip(bool isAvailable) => isAvailable
    ? const StatusChip(label: 'Dostupno', tone: StatusTone.success)
    : const StatusChip(label: 'Nedostupno', tone: StatusTone.neutral);
