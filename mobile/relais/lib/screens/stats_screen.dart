import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class StatsScreen extends StatefulWidget {
  const StatsScreen({super.key});

  @override
  State<StatsScreen> createState() => _StatsScreenState();
}

class _StatsScreenState extends State<StatsScreen> {
  Map<String, dynamic>? _stats;
  List<dynamic>? _historique;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    final api = context.read<ApiService>();
    final stats = await api.getStats();
    final histo = await api.getHistoriqueEspeces();
    if (!mounted) return;
    if (stats.containsKey('error')) {
      setState(() { _error = stats['error']; _loading = false; });
    } else {
      setState(() { _stats = stats; _historique = histo; _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Statistiques'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? ListView(children: [Padding(padding: const EdgeInsets.all(24), child: Text('Erreur : $_error'))])
                : _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    final s = _stats ?? const {};
    final esp = (s['especes'] as Map?) ?? const {};
    final montantDu = (esp['montantDu'] ?? 0).toString();
    final totalEncaisse = (esp['totalEncaisse'] ?? 0).toString();
    final ceMois = (esp['ceMois'] ?? 0).toString();
    final nbDus = (esp['nbEncaissementsDus'] ?? 0).toString();

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Bandeau solde dû
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [AppTheme.danger, AppTheme.warning],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
            borderRadius: BorderRadius.circular(14),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(Icons.account_balance_wallet, color: Colors.white, size: 20),
                  SizedBox(width: 8),
                  Text('Solde espèces dû à la plateforme',
                      style: TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 13)),
                ],
              ),
              const SizedBox(height: 12),
              Text('$montantDu €',
                  style: const TextStyle(color: Colors.white, fontSize: 36, fontWeight: FontWeight.w800)),
              const SizedBox(height: 4),
              Text('$nbDus encaissement(s) à reverser',
                  style: const TextStyle(color: Colors.white70, fontSize: 13)),
            ],
          ),
        ),
        const SizedBox(height: 24),

        const Text('Activité', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
        const SizedBox(height: 12),
        Row(children: [
          Expanded(child: _kpi('Total colis', '${s['totalColis'] ?? 0}', Icons.inventory_2, AppTheme.primary)),
          const SizedBox(width: 12),
          Expanded(child: _kpi('En transit', '${s['enTransit'] ?? 0}', Icons.local_shipping, AppTheme.accent)),
        ]),
        const SizedBox(height: 12),
        Row(children: [
          Expanded(child: _kpi('À retirer', '${s['enAttenteRetrait'] ?? 0}', Icons.hourglass_top, AppTheme.warning)),
          const SizedBox(width: 12),
          Expanded(child: _kpi('Livrés', '${s['livres'] ?? 0}', Icons.check_circle, AppTheme.success)),
        ]),
        const SizedBox(height: 24),

        const Text('Ce mois-ci', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
        const SizedBox(height: 12),
        Row(children: [
          Expanded(child: _kpi('Dépôts', '${s['depotsMois'] ?? 0}', Icons.download_done, AppTheme.success)),
          const SizedBox(width: 12),
          Expanded(child: _kpi('Retraits', '${s['retraitsMois'] ?? 0}', Icons.upload, AppTheme.accent)),
        ]),
        const SizedBox(height: 12),
        _kpi('Espèces ce mois', '$ceMois €', Icons.euro, AppTheme.primary, fullWidth: true),
        const SizedBox(height: 24),

        const Text('Historique espèces', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
        const SizedBox(height: 4),
        Text('Total encaissé : $totalEncaisse € — non reversé : $montantDu €',
            style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
        const SizedBox(height: 12),

        if (_historique == null || _historique!.isEmpty)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 24),
            child: Center(child: Text('Aucun encaissement', style: TextStyle(color: AppTheme.textMuted))),
          )
        else
          ..._historique!.take(20).map((h) => _historiqueItem(h as Map<String, dynamic>)),
        const SizedBox(height: 32),
      ],
    );
  }

  Widget _kpi(String label, String value, IconData icon, Color color, {bool fullWidth = false}) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        border: Border.all(color: AppTheme.border),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Container(
            width: 40, height: 40,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, color: color, size: 22),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: const TextStyle(fontSize: 11, color: AppTheme.textMuted, fontWeight: FontWeight.w600)),
                const SizedBox(height: 2),
                Text(value, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _historiqueItem(Map<String, dynamic> h) {
    final estReverse = h['estReverse'] == true;
    final montant = h['montant']?.toString() ?? '0';
    final dateStr = h['dateEncaissement']?.toString();
    DateTime? date;
    if (dateStr != null) date = DateTime.tryParse(dateStr);
    final dateAffichee = date != null
        ? '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year} ${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}'
        : '—';

    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 14),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        border: Border.all(color: AppTheme.border),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        children: [
          Icon(estReverse ? Icons.check_circle : Icons.schedule,
              color: estReverse ? AppTheme.success : AppTheme.warning, size: 20),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('$montant €', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 15)),
                Text(dateAffichee, style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
            decoration: BoxDecoration(
              color: (estReverse ? AppTheme.success : AppTheme.warning).withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(6),
            ),
            child: Text(
              estReverse ? 'Reversé' : 'À reverser',
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w700,
                color: estReverse ? AppTheme.success : AppTheme.warning,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
