import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'colis_detail_screen.dart';
import 'etapes_screen.dart';

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
                    _info('Poids max trajet', '${t['capaciteMaxPoids']} kg'),
                    _info('Poids restant', '${_calculPoidsRestant()} kg'),
                    if (t['pointDepot'] != null) _info('Point de dépôt', t['pointDepot']),
                    if (t['prixParColis'] != null) _info('Prix/colis', '${t['prixParColis']} €'),
                    if (t['prixAuKilo'] != null) _info('Prix/kg', '${t['prixAuKilo']} €'),
                    const SizedBox(height: 12),
                    if (t['statut'] != 'Termine' && t['dateDemarrageTournee'] == null)
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton.icon(
                          icon: const Icon(Icons.play_circle, size: 18),
                          label: const Text('Démarrer la tournée'),
                          style: ElevatedButton.styleFrom(backgroundColor: AppTheme.success),
                          onPressed: _demarrerTournee,
                        ),
                      ),
                    if (t['dateDemarrageTournee'] != null)
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(color: AppTheme.success.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(8)),
                        child: const Text('✓ Tournée démarrée', textAlign: TextAlign.center,
                            style: TextStyle(color: AppTheme.success, fontWeight: FontWeight.w700)),
                      ),
                    const SizedBox(height: 8),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        icon: const Icon(Icons.route, size: 18),
                        label: const Text('Fiche de tournée'),
                        style: ElevatedButton.styleFrom(backgroundColor: AppTheme.accent),
                        onPressed: () => Navigator.push(context,
                            MaterialPageRoute(builder: (_) => EtapesScreen(
                              trajetId: t['id'],
                              trajetLabel: '${t['villeDepart']} → ${t['villeArrivee']}',
                            ))),
                      ),
                    ),
                    const SizedBox(height: 8),
                    if (t['statut'] != 'Termine')
                      SizedBox(
                        width: double.infinity,
                        child: OutlinedButton.icon(
                          icon: const Icon(Icons.lock_outline, size: 18),
                          label: const Text('Clôturer le trajet'),
                          onPressed: _cloturer,
                        ),
                      ),
                    const SizedBox(height: 8),
                    if (_colis.isNotEmpty) _batchActions(),
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

  String _calculPoidsRestant() {
    final cap = (widget.trajet['capaciteMaxPoids'] as num?)?.toDouble() ?? 0;
    final utilise = _colis.fold<double>(0, (sum, c) {
      final p = (c['poidsDeclare'] as num?)?.toDouble() ?? 0;
      return sum + p;
    });
    return (cap - utilise).toStringAsFixed(1);
  }

  Future<void> _demarrerTournee() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Démarrer la tournée ?'),
        content: const Text('Tous les colis pris en charge passeront en transit. Les clients seront notifiés (dès que les notifications seront activées).'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.success),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Démarrer'),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    final res = await context.read<ApiService>().lancerTournee(widget.trajet['id']);
    if (!mounted) return;
    if (res.containsKey('error')) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['error'])));
    } else {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['message']?.toString() ?? 'Tournée démarrée')));
      setState(() => widget.trajet['dateDemarrageTournee'] = DateTime.now().toIso8601String());
      _load();
    }
  }

  Future<void> _cloturer() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Clôturer ce trajet ?'),
        content: const Text('Le trajet n\'apparaîtra plus dans les recherches et n\'acceptera plus de nouveaux colis.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Clôturer')),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    await context.read<ApiService>().cloturerTrajet(widget.trajet['id']);
    if (!mounted) return;
    Navigator.pop(context, true);
  }

  Widget _batchActions() {
    return Wrap(
      spacing: 8, runSpacing: 8,
      children: [
        _batchBtn('En transit', 'EnTransit', Icons.local_shipping),
        _batchBtn('Arrivé', 'ArriveDansPaysDest', Icons.flag),
        _batchBtn('Au relais', 'ReceptionneParPointRelais', Icons.store),
        _batchBtn('Incident', 'Incident', Icons.warning, danger: true),
      ],
    );
  }

  Widget _batchBtn(String label, String statut, IconData icon, {bool danger = false}) {
    return OutlinedButton.icon(
      icon: Icon(icon, size: 16, color: danger ? AppTheme.danger : AppTheme.primary),
      label: Text(label, style: TextStyle(fontSize: 12, color: danger ? AppTheme.danger : AppTheme.primary)),
      style: OutlinedButton.styleFrom(padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8)),
      onPressed: () => _appliquerBatch(label, statut),
    );
  }

  Future<void> _appliquerBatch(String label, String statut) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Marquer tous les colis : $label ?'),
        content: Text('Cette action s\'applique à tous les colis de ce trajet (sauf ceux déjà refusés, livrés ou annulés).'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Confirmer')),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    final res = await context.read<ApiService>().batchStatutColis(widget.trajet['id'], statut);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['message']?.toString() ?? 'Mis à jour')));
    _load();
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
