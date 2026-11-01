import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../shell/shell_navigation.dart';
import '../../../widgets/dashboard_count.dart';
import '../../../widgets/dashboard_page.dart';
import '../employee/shop_employee_provider.dart';
import '../rental/rental_provider.dart';
import '../store/shop_store_provider.dart';

class EmployeeDashboardScreen extends StatefulWidget {
  const EmployeeDashboardScreen({super.key});

  @override
  State<EmployeeDashboardScreen> createState() =>
      _EmployeeDashboardScreenState();
}

class _EmployeeDashboardScreenState extends State<EmployeeDashboardScreen> {
  final _pendingRentalsCount = DashboardCount();
  final _employeesCount = DashboardCount();
  String? _storeName;

  @override
  void initState() {
    super.initState();
    _pendingRentalsCount.load(
      () => context.read<RentalProvider>().countPending(),
    );
    _employeesCount.load(() async {
      final page = await context.read<ShopEmployeeProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      return page.totalCount;
    });
    _loadStore();
  }

  @override
  void dispose() {
    _pendingRentalsCount.dispose();
    _employeesCount.dispose();
    super.dispose();
  }

  Future<void> _loadStore() async {
    try {
      final store = await context.read<ShopStoreProvider>().getOwnStore();
      if (!mounted) return;
      setState(() {
        _storeName = store.storeName;
      });
    } catch (_) {
      return;
    }
  }

  void _openEntry(String label) {
    ShellNavigation.of(context).openEntry(label);
  }

  @override
  Widget build(BuildContext context) {
    return DashboardPage(
      storeName: _storeName,
      cards: [
        DashboardCountCard(
          count: _pendingRentalsCount,
          icon: Icons.assignment_outlined,
          label: 'Zahtjevi na čekanju',
          tone: StatusTone.warning,
          onTap: () => _openEntry('Zahtjevi'),
        ),
        DashboardCountCard(
          count: _employeesCount,
          icon: Icons.badge_outlined,
          label: 'Zaposlenici',
          tone: StatusTone.info,
        ),
      ],
    );
  }
}
