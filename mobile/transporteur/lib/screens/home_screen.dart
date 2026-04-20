import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'trajets_screen.dart';
import 'colis_screen.dart';
import 'scan_screen.dart';
import 'kyc_screen.dart';
import 'profil_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _index = 0;
  String _kycStatut = 'NonSoumis';
  bool _kycLoading = true;

  final _screens = const [
    TrajetsScreen(),
    ColisScreen(),
    ScanScreen(),
    KycScreen(),
    ProfilScreen(),
  ];

  @override
  void initState() {
    super.initState();
    _checkKyc();
  }

  Future<void> _checkKyc() async {
    final api = context.read<ApiService>();
    final res = await api.getKycStatus();
    if (mounted) {
      setState(() {
        _kycStatut = res['statutKyc']?.toString() ?? 'NonSoumis';
        _kycLoading = false;
        if (_kycStatut != 'Valide') _index = 3; // Force onglet KYC
      });
    }
  }

  bool get _kycValide => _kycStatut == 'Valide';

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        children: [
          if (!_kycLoading && !_kycValide)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              color: AppTheme.accent,
              child: SafeArea(
                bottom: false,
                child: Row(
                  children: [
                    const Icon(Icons.warning_amber, color: Colors.white, size: 20),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        _kycStatut == 'EnAttente'
                            ? 'KYC en attente de validation par l\'admin.'
                            : 'Complétez votre KYC pour publier des trajets.',
                        style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 13),
                      ),
                    ),
                    if (_index != 3)
                      TextButton(
                        onPressed: () => setState(() => _index = 3),
                        child: const Text('Voir', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w800)),
                      ),
                  ],
                ),
              ),
            ),
          Expanded(child: _screens[_index]),
        ],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (i) => setState(() => _index = i),
        backgroundColor: AppTheme.surface,
        indicatorColor: AppTheme.primary.withValues(alpha: 0.1),
        destinations: [
          const NavigationDestination(
            icon: Icon(Icons.local_shipping_outlined),
            selectedIcon: Icon(Icons.local_shipping, color: AppTheme.primary),
            label: 'Trajets',
          ),
          const NavigationDestination(
            icon: Icon(Icons.inventory_2_outlined),
            selectedIcon: Icon(Icons.inventory_2, color: AppTheme.primary),
            label: 'Colis',
          ),
          const NavigationDestination(
            icon: Icon(Icons.qr_code_scanner_outlined),
            selectedIcon: Icon(Icons.qr_code_scanner, color: AppTheme.primary),
            label: 'Scanner',
          ),
          NavigationDestination(
            icon: Icon(
              _kycValide ? Icons.verified_outlined : Icons.warning_amber_outlined,
              color: _kycValide ? null : AppTheme.accent,
            ),
            selectedIcon: Icon(
              _kycValide ? Icons.verified : Icons.warning_amber,
              color: _kycValide ? AppTheme.success : AppTheme.accent,
            ),
            label: 'KYC',
          ),
          const NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person, color: AppTheme.primary),
            label: 'Profil',
          ),
        ],
      ),
    );
  }
}
