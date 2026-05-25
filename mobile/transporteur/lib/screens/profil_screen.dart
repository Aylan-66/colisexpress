import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import '../main.dart';
import 'tarifs_screen.dart';
import 'relais_partenaires_screen.dart';
import 'kyc_screen.dart';
import 'frais_service_screen.dart';

class ProfilScreen extends StatelessWidget {
  const ProfilScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Mon profil')),
      body: FutureBuilder<Map<String, String>>(
        future: _loadUserInfo(),
        builder: (ctx, snap) {
          snap.data;
          return ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Center(
                child: Container(
                  width: 80, height: 80,
                  decoration: BoxDecoration(
                    color: AppTheme.primary,
                    borderRadius: BorderRadius.circular(40),
                  ),
                  child: const Icon(Icons.person, size: 40, color: Colors.white),
                ),
              ),
              const SizedBox(height: 20),

              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Informations', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                      const SizedBox(height: 12),
                      _row('Rôle', 'Transporteur'),
                      _row('Statut', 'Connecté'),
                      const Divider(height: 24),
                      const Text(
                        'Pour modifier vos informations personnelles, rendez-vous sur le site web.',
                        style: TextStyle(fontSize: 12, color: AppTheme.textMuted),
                      ),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 12),

              Card(
                child: Column(
                  children: [
                    _menuTile(context, Icons.euro, 'Mes tarifs', 'Configurer mes templates de prix', const TarifsScreen()),
                    const Divider(height: 1),
                    _menuTile(context, Icons.receipt_long, 'Frais de service', 'Définir mes frais (ou défaut plateforme)', const FraisServiceScreen()),
                    const Divider(height: 1),
                    _menuTile(context, Icons.store, 'Mes points relais', 'Relais associés à mes trajets', const RelaisPartenairesScreen()),
                    const Divider(height: 1),
                    _menuTile(context, Icons.verified, 'KYC', 'Mes documents', KycScreen(onKycValidated: () {})),
                  ],
                ),
              ),

              const SizedBox(height: 12),

              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Application', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                      const SizedBox(height: 12),
                      _row('Version', '1.0.0'),
                      _row('Plateforme', 'Colis Express'),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 24),

              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  icon: const Icon(Icons.logout, size: 18),
                  label: const Text('Se déconnecter'),
                  style: ElevatedButton.styleFrom(backgroundColor: AppTheme.danger),
                  onPressed: () async {
                    await context.read<ApiService>().logout();
                    if (context.mounted) {
                      Navigator.of(context).pushAndRemoveUntil(
                        MaterialPageRoute(builder: (_) => const AuthGate()),
                        (route) => false,
                      );
                    }
                  },
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _menuTile(BuildContext context, IconData icon, String title, String subtitle, Widget page) => ListTile(
        leading: Icon(icon, color: AppTheme.primary),
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.w700)),
        subtitle: Text(subtitle, style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
        trailing: const Icon(Icons.chevron_right, color: AppTheme.textMuted),
        onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => page)),
      );

  Widget _row(String label, String value) => Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Row(
          children: [
            SizedBox(width: 100,
                child: Text(label, style: const TextStyle(color: AppTheme.textMuted, fontSize: 13))),
            Expanded(
                child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13))),
          ],
        ),
      );

  Future<Map<String, String>> _loadUserInfo() async {
    const storage = FlutterSecureStorage();
    return {
      'token': (await storage.read(key: 'accessToken')) ?? '',
    };
  }
}
