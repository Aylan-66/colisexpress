import 'package:flutter/material.dart';
import '../theme.dart';
import 'colis_screen.dart';
import 'scan_screen.dart';
import 'profil_screen.dart';

class HomeScreen extends StatefulWidget {
  final bool showProfilFirst;
  const HomeScreen({super.key, this.showProfilFirst = false});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late int _index;

  final _screens = const [
    ColisScreen(),
    ScanScreen(),
    ProfilScreen(),
  ];

  @override
  void initState() {
    super.initState();
    _index = widget.showProfilFirst ? 2 : 0;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: _screens[_index],
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (i) => setState(() => _index = i),
        backgroundColor: AppTheme.surface,
        indicatorColor: AppTheme.primary.withValues(alpha: 0.1),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.inventory_2_outlined),
            selectedIcon: Icon(Icons.inventory_2, color: AppTheme.primary),
            label: 'Colis',
          ),
          NavigationDestination(
            icon: Icon(Icons.qr_code_scanner_outlined),
            selectedIcon: Icon(Icons.qr_code_scanner, color: AppTheme.primary),
            label: 'Scanner',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person, color: AppTheme.primary),
            label: 'Profil',
          ),
        ],
      ),
    );
  }
}
