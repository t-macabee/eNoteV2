import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';
import 'detail_row.dart';

class StoreProfilePanel extends StatelessWidget {
  final MusicStoreDto store;
  final bool compact;

  const StoreProfilePanel({
    super.key,
    required this.store,
    this.compact = false,
  });

  @override
  Widget build(BuildContext context) {
    final apiClient = context.read<ApiClient>();

    String addressText = '-';
    if (store.addressStreet != null && store.addressStreet!.isNotEmpty) {
      if (store.addressCity != null && store.addressCity!.isNotEmpty) {
        addressText = '${store.addressStreet}, ${store.addressCity}';
      } else {
        addressText = store.addressStreet!;
      }
    } else if (store.addressCity != null && store.addressCity!.isNotEmpty) {
      addressText = store.addressCity!;
    }

    final phoneText =
        (store.phoneNumber != null && store.phoneNumber!.isNotEmpty)
        ? store.phoneNumber!
        : '-';

    final workHoursText = store.businessHours.isNotEmpty
        ? store.businessHours
        : '-';

    return Container(
      color: AppTheme.surfaceContainer,
      child: ListView(
        shrinkWrap: true,
        padding: EdgeInsets.all(AppSpacing.lg),
        children: [
          if (!compact) ...[
            AspectRatio(
              aspectRatio: 1.2,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(12),
                child: networkImageOrPlaceholder(
                  store.imagePath,
                  apiClient,
                  size: double.infinity,
                  borderRadius: 12,
                  placeholder: () => Container(
                    color: AppTheme.background,
                    child: const Center(
                      child: Icon(
                        Icons.storefront_outlined,
                        size: 48,
                        color: AppTheme.textTertiary,
                      ),
                    ),
                  ),
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.lg),
          ],
          Text(
            store.storeName,
            style: Theme.of(context).textTheme.titleLarge
                ?.copyWith(fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: AppSpacing.lg),
          const Divider(),
          const SizedBox(height: AppSpacing.sm),
          DetailRow(
            icon: Icons.location_on_outlined,
            label: 'Adresa',
            value: addressText,
          ),
          DetailRow(
            icon: Icons.phone_outlined,
            label: 'Telefon',
            value: phoneText,
          ),
          DetailRow(
            icon: Icons.access_time_outlined,
            label: 'Radno vrijeme',
            value: workHoursText,
          ),
        ],
      ),
    );
  }
}
