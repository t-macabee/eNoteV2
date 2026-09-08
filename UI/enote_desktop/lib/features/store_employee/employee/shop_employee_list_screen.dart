import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
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

  Future<void> _openCreateForm() async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => const ShopEmployeeFormScreen(
        presentation: EntityFormPresentation.dialog,
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
        fetcher: (page, pageSize, search) => provider.search(
          pagedQuery(page, pageSize, search, searchField: 'name')
        ),
        titleOf: (item) =>
            formatDisplayName(item.firstName, item.lastName, item.username),
        subtitleOf: (item) => item.isManager ? 'Voditelj radnje' : 'Uposlenik radnje',
        placeholderIcon: Icons.badge_outlined,
        imageUrlOf: (item) =>
            userPictureUrl(context.read<ApiClient>(), item.appUserId),
        cardActions: (context, item) {
          final isManager = context.watch<AuthState>().isManager;
          final currentUserId = context.watch<AuthState>().userId;
          if (!isManager || item.appUserId == currentUserId) return const [];
          return [
            IconButton(
              icon: Icon(item.isActive ? Icons.toggle_on : Icons.toggle_off),
              tooltip: item.isActive ? 'Deaktiviraj' : 'Aktiviraj',
              onPressed: () async {
                final confirmed = await confirmDialog(
                  context: context,
                  title: item.isActive ? 'Potvrdite deaktivaciju' : 'Potvrdite aktivaciju',
                  message: item.isActive
                      ? 'Da li ste sigurni da želite da deaktivirate ovog korisnika?'
                      : 'Da li ste sigurni da želite da aktivirate ovog korisnika?',
                );
                if (confirmed != true) return;
                if (!context.mounted) return;
                try {
                  await context.read<ShopEmployeeProvider>().setActive(item.appUserId, !item.isActive);
                  _gridKey.currentState?.refresh();
                } catch (e) {
                  if (context.mounted) ErrorBanner.show(context, message: userMessage(e));
                }
              },
            ),
          ];
        },
        onAdd: isManager ? _openCreateForm : null,
        addLabel: isManager ? 'Kreiraj zaposlenika' : null,
        showAddButton: isManager,
        searchHint: 'Pretraži zaposlenike po imenu ili korisničkom imenu...',
      ),
    );
  }
}
