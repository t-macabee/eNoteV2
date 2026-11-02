import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

class RentalTile extends StatelessWidget {
  final InstrumentRentalDto rental;
  final VoidCallback onTap;

  const RentalTile({super.key, required this.rental, required this.onTap});

  bool get _showsPaidMark =>
      rental.rentalStatus == InstrumentRentalStatus.completed ||
      rental.rentalStatus == InstrumentRentalStatus.returnedEarly;

  @override
  Widget build(BuildContext context) {
    final chips = [
      if (rental.isOverdue)
        StatusChip(
          label: 'Kasni ${rental.overdueDays} dana',
          tone: StatusTone.danger,
        ),
      if (_showsPaidMark)
        (rental.isPaid
            ? StatusChip(
                label: paymentStatusLabel(PaymentStatus.succeeded),
                tone: paymentStatusTone(PaymentStatus.succeeded),
              )
            : const StatusChip(
                label: 'Nije plaćeno',
                tone: StatusTone.warning,
              )),
    ];

    return ListTile(
      onTap: onTap,
      leading: ImageThumbnail(
        imageUrl: rental.instrumentImagePath,
        apiClient: context.read<ApiClient>(),
        size: AppSize.listThumbnail,
        borderRadius: AppRadius.md,
      ),
      title: Text(rental.instrumentModel),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text('${rental.storeName} · ${formatDate(rental.requestedAt)}'),
          if (chips.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xs),
            Wrap(
              spacing: AppSpacing.xs,
              runSpacing: AppSpacing.xs,
              children: chips,
            ),
          ],
        ],
      ),
      trailing: StatusChip(
        label: rentalStatusLabel(rental.rentalStatus),
        tone: rentalStatusTone(rental.rentalStatus),
      ),
    );
  }
}
