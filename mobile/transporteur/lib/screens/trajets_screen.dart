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
  String _filtre = 'Tous';   // Tous / Actif / Complet / Termine
  final _searchCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final api = context.read<ApiService>();
    _trajets = await api.getMesTrajets();
    setState(() => _loading = false);
  }

  List<dynamic> get _trajetsFiltres {
    final q = _searchCtrl.text.trim().toLowerCase();
    return _trajets.where((tr) {
      final t = tr as Map<String, dynamic>;
      if (_filtre != 'Tous' && (t['statut']?.toString() ?? '') != _filtre) return false;
      if (q.isEmpty) return true;
      final dep = (t['villeDepart'] ?? '').toString().toLowerCase();
      final arr = (t['villeArrivee'] ?? '').toString().toLowerCase();
      final date = (t['dateDepart'] ?? '').toString();
      return dep.contains(q) || arr.contains(q) || date.contains(q);
    }).toList();
  }

  Future<void> _cloturer(String id) async {
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
    await context.read<ApiService>().cloturerTrajet(id);
    _load();
  }

  Future<void> _dupliquer(Map<String, dynamic> t) async {
    final origDate = DateTime.tryParse(t['dateDepart']?.toString() ?? '') ?? DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: origDate.add(const Duration(days: 7)),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked == null || !mounted) return;
    final iso = DateTime.utc(picked.year, picked.month, picked.day, origDate.hour, origDate.minute).toIso8601String();
    final res = await context.read<ApiService>().dupliquerTrajet(t['id'], iso);
    if (!mounted) return;
    if (res.containsKey('error')) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['error'])));
    } else {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Trajet dupliqué.')));
      _load();
    }
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
          : Column(
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
                  child: TextField(
                    controller: _searchCtrl,
                    onChanged: (_) => setState(() {}),
                    decoration: InputDecoration(
                      hintText: 'Rechercher (ville, date)…',
                      prefixIcon: const Icon(Icons.search, size: 20),
                      isDense: true,
                      suffixIcon: _searchCtrl.text.isEmpty
                          ? null
                          : IconButton(
                              icon: const Icon(Icons.clear, size: 18),
                              onPressed: () { _searchCtrl.clear(); setState(() {}); }),
                    ),
                  ),
                ),
                SizedBox(
                  height: 40,
                  child: ListView(
                    scrollDirection: Axis.horizontal,
                    padding: const EdgeInsets.symmetric(horizontal: 8),
                    children: ['Tous', 'Actif', 'Complet', 'Termine'].map((f) {
                      final selected = _filtre == f;
                      return Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 4),
                        child: ChoiceChip(
                          label: Text(f),
                          selected: selected,
                          onSelected: (_) => setState(() => _filtre = f),
                        ),
                      );
                    }).toList(),
                  ),
                ),
                Expanded(child: _trajets.isEmpty ? _empty() : _list()),
              ],
            ),
    );
  }

  Widget _list() {
    final filtres = _trajetsFiltres;
    if (filtres.isEmpty) {
      return const Center(child: Padding(padding: EdgeInsets.all(24), child: Text('Aucun trajet correspondant.', style: TextStyle(color: AppTheme.textMuted))));
    }
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: filtres.length,
        itemBuilder: (ctx, i) => _TrajetCard(
          trajet: filtres[i],
          onDelete: () => _deleteTrajet(filtres[i]['id']),
          onCloturer: () => _cloturer(filtres[i]['id']),
          onDupliquer: () => _dupliquer(filtres[i] as Map<String, dynamic>),
          onTap: () => Navigator.push(context,
              MaterialPageRoute(builder: (_) => TrajetDetailScreen(trajet: filtres[i]))),
        ),
      ),
    );
  }

  Widget _empty() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.local_shipping_outlined, size: 64, color: AppTheme.textMuted),
          const SizedBox(height: 12),
          const Text('Aucun trajet', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
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
  final VoidCallback onCloturer;
  final VoidCallback onDupliquer;

  const _TrajetCard({
    required this.trajet,
    required this.onDelete,
    required this.onTap,
    required this.onCloturer,
    required this.onDupliquer,
  });

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
                PopupMenuButton<String>(
                  icon: const Icon(Icons.more_vert, size: 20),
                  onSelected: (v) {
                    if (v == 'dupliquer') onDupliquer();
                    else if (v == 'cloturer') onCloturer();
                    else if (v == 'supprimer') onDelete();
                  },
                  itemBuilder: (_) => const [
                    PopupMenuItem(value: 'dupliquer', child: ListTile(leading: Icon(Icons.copy, size: 18), title: Text('Dupliquer'))),
                    PopupMenuItem(value: 'cloturer', child: ListTile(leading: Icon(Icons.lock_outline, size: 18), title: Text('Clôturer'))),
                    PopupMenuItem(value: 'supprimer', child: ListTile(leading: Icon(Icons.delete_outline, size: 18, color: AppTheme.danger), title: Text('Supprimer', style: TextStyle(color: AppTheme.danger)))),
                  ],
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
