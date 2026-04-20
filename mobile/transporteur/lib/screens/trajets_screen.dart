import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'create_trajet_screen.dart';
import 'trajet_detail_screen.dart';

class TrajetsScreen extends StatefulWidget {
  const TrajetsScreen({super.key});

  @override
  State<TrajetsScreen> createState() => _TrajetsScreenState();
}

class _TrajetsScreenState extends State<TrajetsScreen> {
  List<dynamic> _trajets = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final api = context.read<ApiService>();
    _trajets = await api.getMesTrajets();
    setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Mes trajets'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add_circle, color: AppTheme.accent),
            onPressed: () async {
              final created = await Navigator.push<bool>(context,
                  MaterialPageRoute(builder: (_) => const CreateTrajetScreen()));
              if (created == true) _load();
            },
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _trajets.isEmpty
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.local_shipping_outlined,
                          size: 64, color: AppTheme.textMuted),
                      const SizedBox(height: 12),
                      const Text('Aucun trajet',
                          style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                      const SizedBox(height: 4),
                      const Text('Créez votre premier trajet pour recevoir des colis.',
                          style: TextStyle(color: AppTheme.textMuted, fontSize: 13)),
                      const SizedBox(height: 20),
                      ElevatedButton.icon(
                        icon: const Icon(Icons.add, size: 18),
                        label: const Text('Créer un trajet'),
                        style: ElevatedButton.styleFrom(backgroundColor: AppTheme.accent),
                        onPressed: () async {
                          final created = await Navigator.push<bool>(context,
                              MaterialPageRoute(builder: (_) => const CreateTrajetScreen()));
                          if (created == true) _load();
                        },
                      ),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _trajets.length,
                    itemBuilder: (ctx, i) => _TrajetCard(
                      trajet: _trajets[i],
                      onDelete: () => _deleteTrajet(_trajets[i]['id']),
                      onTap: () => Navigator.push(context,
                          MaterialPageRoute(builder: (_) => TrajetDetailScreen(trajet: _trajets[i]))),
                    ),
                  ),
                ),
    );
  }

  Future<void> _deleteTrajet(String id) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Supprimer ce trajet ?'),
        content: const Text('Cette action est irréversible.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Supprimer', style: TextStyle(color: AppTheme.danger)),
          ),
        ],
      ),
    );
    if (confirm == true) {
      await context.read<ApiService>().deleteTrajet(id);
      _load();
    }
  }
}

class _TrajetCard extends StatelessWidget {
  final Map<String, dynamic> trajet;
  final VoidCallback onDelete;
  final VoidCallback onTap;

  const _TrajetCard({required this.trajet, required this.onDelete, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final statut = trajet['statut'] ?? 'Actif';
    final Color badgeColor;
    switch (statut) {
      case 'Actif': badgeColor = AppTheme.success; break;
      case 'Complet': badgeColor = AppTheme.warning; break;
      case 'Termine': badgeColor = AppTheme.textMuted; break;
      default: badgeColor = AppTheme.danger;
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    '${trajet['villeDepart']} → ${trajet['villeArrivee']}',
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: badgeColor.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(statut,
                      style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: badgeColor)),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                _info(Icons.calendar_today, _formatDate(trajet['dateDepart'])),
                const SizedBox(width: 16),
                _info(Icons.inventory_2, '${trajet['capaciteRestante']}/${trajet['nombreMaxColis']} places'),
                const SizedBox(width: 16),
                _info(Icons.scale, '${trajet['capaciteMaxPoids']} kg max'),
              ],
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                if (trajet['prixParColis'] != null)
                  Text('${trajet['prixParColis']} €/colis',
                      style: const TextStyle(fontWeight: FontWeight.w700, color: AppTheme.primary, fontSize: 15)),
                if (trajet['prixAuKilo'] != null)
                  Text('${trajet['prixAuKilo']} €/kg',
                      style: const TextStyle(fontWeight: FontWeight.w700, color: AppTheme.primary, fontSize: 15)),
                const Spacer(),
                IconButton(
                  icon: const Icon(Icons.delete_outline, color: AppTheme.danger, size: 20),
                  onPressed: onDelete,
                ),
              ],
            ),
          ],
        ),
      ),
      ),
    );
  }

  Widget _info(IconData icon, String text) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: AppTheme.textMuted),
          const SizedBox(width: 4),
          Text(text, style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
        ],
      );

  String _formatDate(String? iso) {
    if (iso == null) return '—';
    final d = DateTime.tryParse(iso);
    if (d == null) return iso;
    return '${d.day}/${d.month}/${d.year}';
  }
}
