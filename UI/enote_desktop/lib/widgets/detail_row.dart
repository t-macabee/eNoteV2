import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

/// An icon + stacked label/value row used in read-only detail panels/dialogs
/// (e.g. store details, instrument details). Factored out of
/// `MusicStoreDetailScreen`, `ShopStoreScreen` and `InstrumentDetailDialog`,
/// which each defined an identical private `_buildInfoRow` method.
///
/// Named `DetailRow` rather than `InfoRow` to avoid colliding with
/// `enote_core`'s `InfoRow` (a different, side-by-side label/value layout
/// already used by `UserGridScreen`/`ProfileDialog`) — the two aren't
/// interchangeable, so this stays a separate widget rather than replacing
/// that one.
class DetailRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const DetailRow({
    super.key,
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: AppTheme.textSecondary),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppTheme.textTertiary,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  style: const TextStyle(
                    fontSize: 14,
                    color: AppTheme.textPrimary,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
