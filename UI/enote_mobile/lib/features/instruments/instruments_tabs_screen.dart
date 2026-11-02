import 'package:flutter/material.dart';

import '../../widgets/section_tabs.dart';
import '../rentals/rental_list_screen.dart';
import 'instrument_catalog_screen.dart';

class InstrumentsTabsScreen extends StatelessWidget {
  const InstrumentsTabsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const SectionTabs(
      tabs: [
        (label: 'Katalog', child: InstrumentCatalogScreen()),
        (label: 'Iznajmljivanja', child: RentalListScreen()),
      ],
    );
  }
}
