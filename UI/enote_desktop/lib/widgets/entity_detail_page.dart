import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class EntityDetailTab {
  final String label;
  final IconData? icon;
  final WidgetBuilder builder;

  const EntityDetailTab({
    required this.label,
    this.icon,
    required this.builder,
  });
}

class EntityDetailPage extends StatelessWidget {
  final String title;
  final List<EntityDetailTab> tabs;
  final List<Widget> actions;
  final VoidCallback? onClose;
  final int initialIndex;

  const EntityDetailPage({
    super.key,
    required this.title,
    required this.tabs,
    this.actions = const [],
    this.onClose,
    this.initialIndex = 0,
  });

  @override
  Widget build(BuildContext context) {
    final tabBar = tabs.length > 1
        ? TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            tabs: tabs
                .map(
                  (tab) => tab.icon == null
                      ? Tab(text: tab.label)
                      : Tab(icon: Icon(tab.icon), text: tab.label),
                )
                .toList(),
          )
        : null;

    return DefaultTabController(
      length: tabs.length,
      initialIndex: initialIndex,
      child: Scaffold(
        appBar: AppBar(
          leading: IconButton(
            icon: const Icon(Icons.close),
            tooltip: 'Zatvori',
            onPressed: onClose ?? () => Navigator.of(context).pop(),
          ),
          title: Text(title, overflow: TextOverflow.ellipsis),
          actions: actions,
          bottom: tabBar == null
              ? null
              : PreferredSize(
                  preferredSize: tabBar.preferredSize,
                  child: Center(
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(
                        maxWidth: AppSize.contentMaxWidth,
                      ),
                      child: tabBar,
                    ),
                  ),
                ),
        ),
        body: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(
              maxWidth: AppSize.contentMaxWidth,
            ),
            child: TabBarView(
              children: tabs
                  .map((tab) => _KeepAliveTab(builder: tab.builder))
                  .toList(),
            ),
          ),
        ),
      ),
    );
  }
}

class _KeepAliveTab extends StatefulWidget {
  final WidgetBuilder builder;

  const _KeepAliveTab({required this.builder});

  @override
  State<_KeepAliveTab> createState() => _KeepAliveTabState();
}

class _KeepAliveTabState extends State<_KeepAliveTab>
    with AutomaticKeepAliveClientMixin {
  @override
  bool get wantKeepAlive => true;

  @override
  Widget build(BuildContext context) {
    super.build(context);
    return widget.builder(context);
  }
}
