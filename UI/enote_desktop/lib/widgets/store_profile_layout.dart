import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import 'store_profile_panel.dart';

class StoreProfileLayout extends StatelessWidget {
  final MusicStoreDto store;
  final Widget content;

  const StoreProfileLayout({
    super.key,
    required this.store,
    required this.content,
  });

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth >= AppSize.storeProfileStackBreakpoint) {
          return Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SizedBox(
                width: AppSize.storeProfilePanelWidth,
                child: StoreProfilePanel(store: store),
              ),
              const VerticalDivider(width: 1),
              Expanded(child: content),
            ],
          );
        }
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Flexible(child: StoreProfilePanel(store: store, compact: true)),
            const Divider(height: 1),
            Expanded(flex: 2, child: content),
          ],
        );
      },
    );
  }
}
