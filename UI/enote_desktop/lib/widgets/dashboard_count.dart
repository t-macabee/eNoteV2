import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import 'dashboard_stat_card.dart';

class DashboardCount extends ChangeNotifier {
  int? _value;
  bool _isLoading = true;
  bool _hasError = false;
  bool _disposed = false;

  int? get value => _value;
  bool get isLoading => _isLoading;
  bool get hasError => _hasError;

  Future<void> load(Future<int?> Function() fetch) async {
    try {
      _value = await fetch();
      _hasError = false;
    } catch (_) {
      _hasError = true;
    }
    _isLoading = false;
    if (!_disposed) notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}

class DashboardCountCard extends StatelessWidget {
  final DashboardCount count;
  final IconData icon;
  final String label;
  final StatusTone tone;
  final VoidCallback? onTap;

  const DashboardCountCard({
    super.key,
    required this.count,
    required this.icon,
    required this.label,
    this.tone = StatusTone.accent,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: count,
      builder: (context, _) {
        return DashboardStatCard(
          icon: icon,
          label: label,
          value: count.value,
          isLoading: count.isLoading,
          hasError: count.hasError,
          tone: tone,
          onTap: onTap,
        );
      },
    );
  }
}
