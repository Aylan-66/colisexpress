import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'colis_screen.dart';
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
        if (!_kycValide) _index = 0; // KYC tab
      });
    }
  }

  bool get _kycValide => _kycStatut == 'Valide';

  void _onKycValidated() {
    setState(() {
      _kycStatut = 'Valide';
      _index = 0; // Go to Colis
    });
  }

  List<Widget> get _screens => _kycValide
      ? [
          const ColisScreen(),
          const ProfilScreen(),
          KycScreen(onKycValidated: _onKycValidated),
        ]
      : [
          KycScreen(onKycValidated: _onKycValidated),
          const ProfilScreen(),
        ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        children: [
          if (!_kycLoading && !_kycValide)
            Container(
              width: double.infinity,
              padding:
                  const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              color: AppTheme.accent,
              child: SafeArea(
                bottom: false,
                child: Row(
                  children: [
                    const Icon(Icons.warning_amber,
                        color: Colors.white, size: 20),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        _kycStatut == 'EnAttente'
                            ? 'KYC en attente de validation par l\'admin. Vous ne pouvez pas encore utiliser l\'app.'
                            : 'Completez et soumettez votre KYC pour acceder a toutes les fonctionnalites.',
                        style: const TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.w600,
                            fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          Expanded(child: _screens[_index]),
        ],
      ),
      bottomNavigationBar: _kycValide
          ? NavigationBar(
              selectedIndex: _index,
              onDestinationSelected: (i) => setState(() => _index = i),
              backgroundColor: AppTheme.surface,
              indicatorColor: AppTheme.primary.withValues(alpha: 0.1),
              destinations: const [
                NavigationDestination(
                  icon: Icon(Icons.inventory_2_outlined),
                  selectedIcon:
                      Icon(Icons.inventory_2, color: AppTheme.primary),
                  label: 'Colis',
                ),
                NavigationDestination(
                  icon: Icon(Icons.person_outline),
                  selectedIcon:
                      Icon(Icons.person, color: AppTheme.primary),
                  label: 'Profil',
                ),
                NavigationDestination(
                  icon: Icon(Icons.verified_outlined),
                  selectedIcon:
                      Icon(Icons.verified, color: AppTheme.success),
                  label: 'KYC',
                ),
              ],
            )
          : NavigationBar(
              selectedIndex: _index,
              onDestinationSelected: (i) => setState(() => _index = i),
              backgroundColor: AppTheme.surface,
              indicatorColor: AppTheme.accent.withValues(alpha: 0.1),
              destinations: [
                NavigationDestination(
                  icon: Icon(Icons.warning_amber_outlined,
                      color: AppTheme.accent),
                  selectedIcon:
                      Icon(Icons.warning_amber, color: AppTheme.accent),
                  label: 'KYC',
                ),
                const NavigationDestination(
                  icon: Icon(Icons.person_outline),
                  selectedIcon:
                      Icon(Icons.person, color: AppTheme.primary),
                  label: 'Profil',
                ),
              ],
            ),
    );
  }
}
