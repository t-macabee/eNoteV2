import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/entity_list_screen.dart';
import '../address/address_list_screen.dart';
import '../city/city_list_screen.dart';
import '../instrument_type/instrument_type_list_screen.dart';

/// A tabbed dialog unifying reference data management for
/// Gradovi, Adrese, and Tipovi instrumenata.
class ReferenceDataDialog extends StatelessWidget {
  const ReferenceDataDialog({super.key});

  @override
  Widget build(BuildContext context) {
    return Dialog(
      clipBehavior: Clip.antiAlias,
      insetPadding: const EdgeInsets.symmetric(horizontal: 48, vertical: 36),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: ConstrainedBox(
        constraints: const BoxConstraints(
          maxWidth: 960,
          maxHeight: 720,
        ),
        child: DefaultTabController(
          length: 3,
          child: Column(
            children: [
              Container(
                decoration: const BoxDecoration(
                  border: Border(
                    bottom: BorderSide(
                      color: AppTheme.outline,
                      width: 1,
                    ),
                  ),
                ),
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: Row(
                  children: [
                    const Expanded(
                      child: TabBar(
                        isScrollable: true,
                        tabAlignment: TabAlignment.start,
                        tabs: [
                          Tab(text: 'Gradovi'),
                          Tab(text: 'Adrese'),
                          Tab(text: 'Tipovi instrumenata'),
                        ],
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.close),
                      tooltip: 'Zatvori',
                      onPressed: () => Navigator.of(context).pop(),
                    ),
                  ],
                ),
              ),
              const Expanded(
                child: TabBarView(
                  children: [
                    CityListScreen(
                      presentation: EntityListPresentation.embedded,
                      listStyle: EntityListStyle.tiles,
                    ),
                    AddressListScreen(
                      presentation: EntityListPresentation.embedded,
                      listStyle: EntityListStyle.tiles,
                    ),
                    InstrumentTypeListScreen(
                      presentation: EntityListPresentation.embedded,
                      listStyle: EntityListStyle.tiles,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
