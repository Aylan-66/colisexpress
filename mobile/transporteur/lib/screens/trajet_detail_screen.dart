import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'colis_detail_screen.dart';

class TrajetDetailScreen extends StatefulWidget {
  final Map<String, dynamic> trajet;
  const TrajetDetailScreen({super.key, required this.trajet});

  @override
  State<TrajetDetailScreen> createState() => _TrajetDetailScreenState();
}

class _TrajetDetailScreenState extends State<TrajetDetailScreen> {
  List<dynamic> _colis = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final id = widget.trajet['id'];
    _colis = await context.read<ApiService>().getColisForTrajet(id);
    setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    final t = widget.trajet;
    return Scaffold(
      appBar: AppBar(title: Text('${t['villeDepart']} → ${t['villeArrivee']}')),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('${t['villeDepart']} → ${t['villeArrivee']}',
                        style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                    const SizedBox(height: 10),
                    _info('Départ', _fmtDate(t['dateDepart'])),
                    _info('Arrivée', _fmtDate(t['dateEstimeeArrivee'])),
                    _info('Capacité', '${t['capaciteRestante']}/${t['nombreMaxColis']} places'),
                    _info('Poids max', '${t['capaciteMaxPoids']} kg'),
                    if (t['pointDepot'] != null) _info('Point de dépôt', t['pointDepot']),
                    if (t['prixParColis'] != null) _info('Prix/colis', '${t['prixParColis']} €'),
                    if (t['prixAuKilo'] != null) _info('Prix/kg', '${t['prixAuKilo']} €'),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 16),
            Text('COLIS SUR CE TRAJET (${_colis.length})',
                style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
            const SizedBox(height: 10),

            if (_loading)
              const Center(child: Padding(padding: EdgeInsets.all(32), child: CircularProgressIndicator()))
            else if (_colis.isEmpty)
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Center(
                    child: Text('Aucun colis réservé sur ce trajet',
                        style: TextStyle(color: AppTheme.textMuted, fontSize: 13)),
                  ),
                ),
              )
            else
              ..._colis.map((c) => _colisCard(c)),
          ],
        ),
      ),
    );
  }

  Widget _colisCard(Map<String, dynamic> c) {
    final code = c['codeColis'] ?? '—';
    final statut = c['statut'] ?? '—';
    final dest = c['nomDestinataire'] ?? '—';
    final poids = c['poidsDeclare']?.toString() ?? '—';

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        onTap: () => Navigator.push(context,
            MaterialPageRoute(builder: (_) => ColisDetailScreen(codeColis: code))),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        leading: Container(
          width: 44, height: 44,
          decoration: BoxDecoration(
            color: AppTheme.accent.withValues(alpha: 0.15),
            borderRadius: BorderRadius.circular(10),
          ),
          child: const Icon(Icons.inventory_2, color: AppTheme.accent, size: 22),
        ),
        title: Text(code, style: const TextStyle(fontWeight: FontWeight.w700, fontFamily: 'monospace', fontSize: 13)),
        subtitle: Text('$dest • $poids kg', style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
        trailing: Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: AppTheme.primary.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Text(statut, style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: AppTheme.primary)),
        ),
      ),
    );
  }

  Widget _info(String label, String value) => Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: Row(
          children: [
            SizedBox(width: 110, child: Text(label, style: const TextStyle(color: AppTheme.textMuted, fontSize: 13))),
            Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13))),
          ],
        ),
      );

  String _fmtDate(String? iso) {
    if (iso == null) return '—';
    final d = DateTime.tryParse(iso);
    if (d == null) return iso;
    return '${d.day}/${d.month}/${d.year}';
  }
}
