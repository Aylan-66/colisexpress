import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'colis_detail_screen.dart';

class ColisScreen extends StatefulWidget {
  const ColisScreen({super.key});

  @override
  State<ColisScreen> createState() => _ColisScreenState();
}

class _ColisScreenState extends State<ColisScreen> {
  final _codeCtrl = TextEditingController();
  List<dynamic> _commandes = [];
  bool _loading = true;
  String _filtre = 'Tous';
  static const int _pageSize = 20;
  int _visibleCount = _pageSize;

  // Filtres disponibles → liste de statuts correspondants
  static const Map<String, List<String>> _filtres = {
    'Tous': [],
    'Déposé client': ['DeposeParClient'],
    'En transit': ['ReceptionneParTransporteur', 'PhotoPriseEnChargeEnregistree', 'EnTransit'],
    'Déposé relais': ['ArriveDansPaysDest', 'ReceptionneParPointRelais', 'DisponibleAuRetrait'],
    'Récupéré client': ['RetireParDestinataire', 'LivraisonCloturee'],
    'Incident': ['Incident', 'Endommage', 'Perdu', 'RetourExpediteur'],
    'Refusé': ['Refuse'],
  };

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    _commandes = await context.read<ApiService>().getMesCommandesTransporteur();
    if (mounted) setState(() => _loading = false);
  }

  List<dynamic> get _filtered {
    final statuts = _filtres[_filtre] ?? [];
    if (statuts.isEmpty) return _commandes;
    return _commandes.where((c) {
      final s = (c['statutColis'] ?? '').toString();
      return statuts.contains(s);
    }).toList();
  }

  void _searchByCode() {
    final code = _codeCtrl.text.trim();
    if (code.isEmpty) return;
    Navigator.push(context,
        MaterialPageRoute(builder: (_) => ColisDetailScreen(codeColis: code)));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Colis')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _codeCtrl,
                    decoration: const InputDecoration(
                      labelText: 'CODE COLIS',
                      hintText: 'COL-2026-0001',
                      prefixIcon: Icon(Icons.search, size: 20),
                      isDense: true,
                    ),
                    textInputAction: TextInputAction.search,
                    onSubmitted: (_) => _searchByCode(),
                  ),
                ),
                const SizedBox(width: 8),
                ElevatedButton(onPressed: _searchByCode, child: const Text('Voir')),
              ],
            ),
          ),
          SizedBox(
            height: 44,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 12),
              children: _filtres.keys.map((f) {
                final selected = _filtre == f;
                return Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 6),
                  child: ChoiceChip(
                    label: Text(f),
                    selected: selected,
                    onSelected: (_) => setState(() { _filtre = f; _visibleCount = _pageSize; }),
                  ),
                );
              }).toList(),
            ),
          ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _filtered.isEmpty
                    ? Center(
                        child: Text(_filtre == 'Tous' ? 'Aucun colis en cours' : 'Aucun colis pour ce filtre',
                            style: const TextStyle(color: AppTheme.textMuted)),
                      )
                    : Builder(builder: (ctx) {
                        final all = _filtered;
                        final visibles = all.take(_visibleCount).toList();
                        final hasMore = all.length > _visibleCount;
                        return RefreshIndicator(
                          onRefresh: _load,
                          child: ListView.builder(
                            padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
                            itemCount: visibles.length + (hasMore ? 1 : 0),
                            itemBuilder: (ctx, i) {
                              if (i >= visibles.length) {
                                return Padding(
                                  padding: const EdgeInsets.symmetric(vertical: 8),
                                  child: OutlinedButton(
                                    onPressed: () => setState(() => _visibleCount += _pageSize),
                                    child: Text('Voir plus (${all.length - _visibleCount} restants)'),
                                  ),
                                );
                              }
                              final c = visibles[i] as Map<String, dynamic>;
                              return _ColisListItem(
                                commande: c,
                                onTap: () {
                                  final code = c['codeColis'] ?? '';
                                  if (code.isNotEmpty) {
                                    Navigator.push(context,
                                        MaterialPageRoute(builder: (_) => ColisDetailScreen(codeColis: code)));
                                  }
                                },
                              );
                            },
                          ),
                        );
                      }),
          ),
        ],
      ),
    );
  }
}

class _ColisListItem extends StatelessWidget {
  final Map<String, dynamic> commande;
  final VoidCallback onTap;

  const _ColisListItem({required this.commande, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final statut = commande['statutColis']?.toString() ?? '—';
    final code = commande['codeColis'] ?? '—';
    final trajet = commande['trajet'] ?? '—';
    final dateArr = commande['dateArriveePrevue']?.toString();
    final (color, label) = _statutInfo(statut);

    String? arriveeStr;
    if (dateArr != null) {
      final d = DateTime.tryParse(dateArr);
      if (d != null) {
        final l = d.toLocal();
        arriveeStr = 'Arrivée prévue : ${l.day}/${l.month}/${l.year}';
      }
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        onTap: onTap,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        leading: Container(
          width: 44, height: 44,
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Icon(Icons.inventory_2, color: color, size: 22),
        ),
        title: Text(code,
            style: const TextStyle(fontWeight: FontWeight.w700, fontFamily: 'monospace', fontSize: 14)),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(trajet, style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
            if (arriveeStr != null)
              Text(arriveeStr, style: const TextStyle(fontSize: 11, color: AppTheme.textMuted)),
          ],
        ),
        trailing: Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.12),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Text(label,
              style: TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: color)),
        ),
      ),
    );
  }

  (Color, String) _statutInfo(String s) => switch (s) {
        'EnAttenteValidationTransporteur' => (AppTheme.accent, 'À valider'),
        'EnAttenteDepot' => (AppTheme.accent, 'Attente dépôt'),
        'DeposeParClient' => (AppTheme.accent, 'Déposé client'),
        'ReceptionneParTransporteur' || 'PhotoPriseEnChargeEnregistree' => (AppTheme.primary, 'Pris en charge'),
        'EnTransit' => (AppTheme.primary, 'En transit'),
        'ArriveDansPaysDest' => (AppTheme.primary, 'Arrivé'),
        'ReceptionneParPointRelais' => (AppTheme.primary, 'Au relais'),
        'DisponibleAuRetrait' => (AppTheme.success, 'Dispo retrait'),
        'RetireParDestinataire' || 'LivraisonCloturee' => (AppTheme.success, 'Livré'),
        'Incident' || 'Endommage' || 'Perdu' || 'RetourExpediteur' => (AppTheme.danger, 'Incident'),
        'Refuse' => (AppTheme.danger, 'Refusé'),
        _ => (AppTheme.textMuted, s),
      };
}
