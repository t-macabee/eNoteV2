import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';
import 'detail_row.dart';

/// Shared store profile panel used by the shop's own-store screen and the
/// admin music-store detail screen: cover image, name, and address / phone /
/// hours rows.
///
/// The two callers were near-identical; the only difference is the admin
/// action section (Uredi / Obriši buttons). It is gated behind [showActions]
/// (default off, preserving the shop's read-only rendering) with explicit
/// [onEdit]/[onDelete] callbacks so no caller changes appearance.
class StoreProfilePanel extends StatelessWidget {
  final MusicStoreDto store;
  final bool showActions;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;

  const StoreProfilePanel({
    super.key,
    required this.store,
    this.showActions = false,
    this.onEdit,
    this.onDelete,
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

    final workHoursText =
        store.businessHours.isNotEmpty ? store.businessHours : '-';

    return Container(
      color: AppTheme.surfaceContainer,
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
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
            const SizedBox(height: 16),
            Text(
              store.storeName,
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 16),
            const Divider(),
            const SizedBox(height: 8),
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
            if (showActions) ...[
              const SizedBox(height: 24),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: onEdit,
                  icon: const Icon(Icons.edit_outlined, size: 18),
                  label: const Text('Uredi'),
                ),
              ),
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: onDelete,
                  icon: const Icon(Icons.delete_outline, size: 18),
                  label: const Text('Obriši'),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: AppTheme.error,
                    side: const BorderSide(color: AppTheme.error),
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
