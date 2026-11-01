import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../shell/shell_navigation.dart';
import '../../../widgets/dashboard_page.dart';
import '../../../widgets/dashboard_stat_card.dart';
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
  int? _pendingRentalsCount;
  int? _employeesCount;
  String? _storeName;

  bool _pendingRentalsLoading = true;
  bool _employeesLoading = true;

  bool _pendingRentalsFailed = false;
  bool _employeesFailed = false;

  @override
  void initState() {
    super.initState();
    _loadPendingRentals();
    _loadEmployees();
    _loadStore();
  }

  Future<void> _loadPendingRentals() async {
    try {
      final page = await context.read<RentalProvider>().getPage(
        params: pagedQuery(
          1,
          1,
          '',
          filters: {'rentalStatus': InstrumentRentalStatus.pending.toJson()},
        ),
      );
      if (!mounted) return;
      setState(() {
        _pendingRentalsCount = page.totalCount;
        _pendingRentalsLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _pendingRentalsFailed = true;
        _pendingRentalsLoading = false;
      });
    }
  }

  Future<void> _loadEmployees() async {
    try {
      final page = await context.read<ShopEmployeeProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      if (!mounted) return;
      setState(() {
        _employeesCount = page.totalCount;
        _employeesLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _employeesFailed = true;
        _employeesLoading = false;
      });
    }
  }

  Future<void> _loadStore() async {
    try {
      final store = await context.read<ShopStoreProvider>().getOwnStore();
      if (!mounted) return;
      setState(() {
        _storeName = store.storeName;
      });
    } catch (_) {
      if (!mounted) return;
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
        DashboardStatCard(
          icon: Icons.assignment_outlined,
          label: 'Zahtjevi na čekanju',
          value: _pendingRentalsCount,
          isLoading: _pendingRentalsLoading,
          hasError: _pendingRentalsFailed,
          tone: StatusTone.warning,
          onTap: () => _openEntry('Zahtjevi'),
        ),
        DashboardStatCard(
          icon: Icons.badge_outlined,
          label: 'Zaposlenici',
          value: _employeesCount,
          isLoading: _employeesLoading,
          hasError: _employeesFailed,
          tone: StatusTone.info,
          onTap: () => _openEntry('Zaposlenici'),
        ),
      ],
      shortcuts: [
        DashboardShortcut(
          label: 'Zahtjevi',
          icon: Icons.assignment_outlined,
          onTap: () => _openEntry('Zahtjevi'),
        ),
        DashboardShortcut(
          label: 'Zaposlenici',
          icon: Icons.badge_outlined,
          onTap: () => _openEntry('Zaposlenici'),
        ),
        DashboardShortcut(
          label: 'Objave',
          icon: Icons.campaign_outlined,
          onTap: () => _openEntry('Objave'),
        ),
      ],
    );
  }
}
