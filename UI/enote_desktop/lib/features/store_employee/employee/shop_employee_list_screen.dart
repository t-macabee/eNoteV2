import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_grid_screen.dart';
import 'shop_employee_form_screen.dart';
import 'shop_employee_provider.dart';

class ShopEmployeeListScreen extends StatefulWidget {
  const ShopEmployeeListScreen({super.key});

  @override
  State<ShopEmployeeListScreen> createState() => _ShopEmployeeListScreenState();
}

class _ShopEmployeeListScreenState extends State<ShopEmployeeListScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<ShopEmployeeDto>>();

  static String _formatDisplayName(
    String? firstName,
    String? lastName,
    String? username,
  ) {
    final name = '${firstName ?? ''} ${lastName ?? ''}'.trim();
    if (name.isNotEmpty) return name;
    return username ?? '-';
  }

  Future<void> _openCreateForm() async {
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => const ShopEmployeeFormScreen(),
      ),
    );
    _gridKey.currentState?.refresh();
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<ShopEmployeeProvider>();
    final isManager = context.watch<AuthState>().isManager;

    return EntityGridScreen<ShopEmployeeDto>(
      key: _gridKey,
      config: EntityGridConfig<ShopEmployeeDto>(
        title: 'Zaposlenici',
        fetcher: (page, pageSize, search) => provider.search(
          search.isEmpty
              ? {'page': page, 'pageSize': pageSize}
              : {'name': search, 'page': page, 'pageSize': pageSize},
        ),
        titleOf: (item) =>
            _formatDisplayName(item.firstName, item.lastName, item.username),
        subtitleOf: (item) => item.isManager ? 'Voditelj radnje' : 'Uposlenik radnje',
        placeholderIcon: Icons.badge_outlined,
        onAdd: isManager ? _openCreateForm : null,
        addLabel: isManager ? 'Kreiraj zaposlenika' : null,
        showAddButton: isManager,
        searchHint: 'Pretraži zaposlenike po imenu ili korisničkom imenu...',
      ),
    );
  }
}
