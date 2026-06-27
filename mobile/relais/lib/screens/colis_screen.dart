import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class ColisScreen extends StatefulWidget {
  const ColisScreen({super.key});

  @override
  State<ColisScreen> createState() => _ColisScreenState();
}

class _ColisScreenState extends State<ColisScreen> {
  List<dynamic> _colisList = [];
  bool _loading = true;
  final _searchCtrl = TextEditingController();
  String _search = '';
  String _filtre = 'Tous'; // Tous / Cote client / Cote transporteur / Disponible retrait
  DateTime? _filtreDate;   // filtre par date de dépôt ou retrait
  static const int _pageSize = 20;
  int _visibleCount = _pageSize;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    _colisList = await context.read<ApiService>().getColisList();
    if (mounted) setState(() => _loading = false);
  }

  bool _matchDate(Map<String, dynamic> c, DateTime jour) {
    bool sameDay(String? iso) {
      if (iso == null) return false;
      final d = DateTime.tryParse(iso);
      if (d == null) return false;
      final l = d.toLocal();
      return l.year == jour.year && l.month == jour.month && l.day == jour.day;
    }
    return sameDay(c['dateDepotClient']?.toString())
        || sameDay(c['dateDepotTransporteur']?.toString())
        || sameDay(c['dateRetrait']?.toString())
        || sameDay(c['dateCreation']?.toString());
  }

  List<dynamic> get _filtered {
    final q = _search.toLowerCase();
    return _colisList.where((c) {
      final code = (c['codeColis'] ?? '').toString().toLowerCase();
      final dest = (c['nomDestinataire'] ?? '').toString().toLowerCase();
      final statut = (c['statut'] ?? '').toString();
      final trajet = (c['trajet'] ?? '').toString().toLowerCase();

      // Filtre par catégorie
      const cotClient = {'EnAttenteDepot', 'EnAttenteReglement', 'DeposeParClient', 'DemandeCreee', 'ReservationConfirmee', 'CodeColisGenere', 'EnAttenteValidationTransporteur'};
      const cotTransporteur = {'ReceptionneParTransporteur', 'PhotoPriseEnChargeEnregistree', 'EnTransit', 'ArriveDansPaysDest', 'ReceptionneParPointRelais'};
      const dispoRetrait = {'DisponibleAuRetrait'};

      switch (_filtre) {
        case 'Côté client':
          if (!cotClient.contains(statut)) return false;
          break;
        case 'Côté transporteur':
          if (!cotTransporteur.contains(statut)) return false;
          break;
        case 'À retirer':
          if (!dispoRetrait.contains(statut)) return false;
          break;
      }

      if (_filtreDate != null && !_matchDate(c as Map<String, dynamic>, _filtreDate!)) return false;

      if (q.isEmpty) return true;
      return code.contains(q) || dest.contains(q) || statut.toLowerCase().contains(q)
          || trajet.contains(q);
    }).toList();
  }

  Future<void> _pickFiltreDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _filtreDate ?? DateTime.now(),
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
      helpText: 'Filtrer par date (dépôt, retrait ou création)',
    );
    if (picked != null) setState(() { _filtreDate = picked; _visibleCount = _pageSize; });
  }

  Future<void> _confirmerDepot(String codeColis) async {
    final api = context.read<ApiService>();
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirmer le depot'),
        content: Text(
            'Confirmez-vous la reception du colis $codeColis dans votre point relais ?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Annuler')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Confirmer')),
        ],
      ),
    );
    if (confirm != true) return;

    final res = await api.confirmerDepot(codeColis);
    if (!mounted) return;

    if (res.containsKey('error')) {
      _showSnackbar(res['error'], AppTheme.danger);
    } else {
      _showSnackbar('Depot confirme pour $codeColis', AppTheme.success);
      _load();
    }
  }

  Future<void> _confirmerRetrait(String codeColis) async {
    final api = context.read<ApiService>();
    final codeCtrl = TextEditingController();
    final code = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirmer le retrait'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Colis : $codeColis',
                style: const TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 12),
            TextField(
              controller: codeCtrl,
              decoration: const InputDecoration(
                labelText: 'CODE DE RETRAIT (4 chiffres)',
                hintText: '0000',
              ),
              keyboardType: TextInputType.number,
              maxLength: 4,
              autofocus: true,
            ),
          ],
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Annuler')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, codeCtrl.text.trim()),
              child: const Text('Valider le retrait')),
        ],
      ),
    );

    if (code == null || code.isEmpty) return;

    final res = await api.confirmerRetrait(codeColis, code);
    if (!mounted) return;

    if (res.containsKey('error')) {
      _showSnackbar(res['error'], AppTheme.danger);
    } else {
      _showSnackbar(
          'Retrait confirme pour $codeColis', AppTheme.success);
      _load();
    }
  }

  void _showSnackbar(String msg, Color color) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(msg, style: const TextStyle(fontWeight: FontWeight.w600)),
        backgroundColor: color,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Colis au relais')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _searchCtrl,
                    decoration: const InputDecoration(
                      labelText: 'RECHERCHER', hintText: 'Code, destinataire, trajet...',
                      prefixIcon: Icon(Icons.search, size: 20),
                    ),
                    onChanged: (v) => setState(() { _search = v; _visibleCount = _pageSize; }),
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(
                  icon: Icon(Icons.calendar_month, color: _filtreDate != null ? AppTheme.accent : null),
                  tooltip: 'Filtrer par date',
                  onPressed: _pickFiltreDate,
                ),
              ],
            ),
          ),
          if (_filtreDate != null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Row(
                children: [
                  Chip(
                    label: Text('${_filtreDate!.day}/${_filtreDate!.month}/${_filtreDate!.year}'),
                    onDeleted: () => setState(() => _filtreDate = null),
                    deleteIcon: const Icon(Icons.close, size: 16),
                  ),
                ],
              ),
            ),
          SizedBox(
            height: 44,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 12),
              children: ['Tous', 'Côté client', 'Côté transporteur', 'À retirer'].map((f) {
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
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.inventory_2_outlined,
                                size: 64, color: AppTheme.textMuted.withValues(alpha: 0.4)),
                            const SizedBox(height: 16),
                            Text(_search.isEmpty ? 'Aucun colis pour le moment' : 'Aucun résultat',
                                style: const TextStyle(color: AppTheme.textMuted, fontSize: 15, fontWeight: FontWeight.w600)),
                          ],
                        ),
                      )
                    : Builder(builder: (ctx) {
                        final all = _filtered;
                        final visibles = all.take(_visibleCount).toList();
                        final hasMore = all.length > _visibleCount;
                        return RefreshIndicator(
                          onRefresh: _load,
                          child: ListView.builder(
                            padding: const EdgeInsets.all(16),
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
                              final colis = visibles[i] as Map<String, dynamic>;
                              return _ColisCard(colis: colis);
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

class _ColisCard extends StatelessWidget {
  final Map<String, dynamic> colis;

  const _ColisCard({required this.colis});

  @override
  Widget build(BuildContext context) {
    final code = colis['codeColis']?.toString() ?? '-';
    final statut = colis['statut']?.toString() ?? colis['statutColis']?.toString() ?? '-';
    final destinataire = colis['nomDestinataire']?.toString() ?? '-';
    final trajet = colis['trajet']?.toString() ?? '';

    final (Color statusColor, String statusLabel) = _statusInfo(statut);

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            Row(
              children: [
                Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    color: AppTheme.primary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.inventory_2,
                      color: AppTheme.primary, size: 22),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(code,
                          style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              fontFamily: 'monospace',
                              fontSize: 15)),
                      if (trajet.isNotEmpty)
                        Text(trajet,
                            style: const TextStyle(
                                fontSize: 12,
                                color: AppTheme.textMuted)),
                    ],
                  ),
                ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: statusColor.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(statusLabel,
                      style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.w700,
                          color: statusColor)),
                ),
              ],
            ),

            const Divider(height: 24),

            // Destinataire
            Row(
              children: [
                const Icon(Icons.person_outline,
                    size: 16, color: AppTheme.textMuted),
                const SizedBox(width: 6),
                Text('Destinataire : $destinataire',
                    style: const TextStyle(
                        fontSize: 13, color: AppTheme.textMuted)),
              ],
            ),

            // Dates clés
            _dateLine(Icons.download_done, 'Dépôt client', colis['dateDepotClient']),
            _dateLine(Icons.local_shipping, 'Dépôt transporteur', colis['dateDepotTransporteur']),
            _dateLine(Icons.outbox, 'Retrait destinataire', colis['dateRetrait']),

            const SizedBox(height: 8),
            const Text('Utilisez l\'onglet Scanner pour changer le statut',
                style: TextStyle(fontSize: 11, color: AppTheme.textMuted, fontStyle: FontStyle.italic)),
          ],
        ),
      ),
    );
  }

  Widget _dateLine(IconData icon, String label, dynamic iso) {
    if (iso == null) return const SizedBox.shrink();
    final d = DateTime.tryParse(iso.toString());
    if (d == null) return const SizedBox.shrink();
    final l = d.toLocal();
    final dateStr = '${l.day.toString().padLeft(2, '0')}/${l.month.toString().padLeft(2, '0')}/${l.year} ${l.hour.toString().padLeft(2, '0')}:${l.minute.toString().padLeft(2, '0')}';
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Row(
        children: [
          Icon(icon, size: 14, color: AppTheme.textMuted),
          const SizedBox(width: 6),
          Text('$label : $dateStr', style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
        ],
      ),
    );
  }

  (Color, String) _statusInfo(String statut) {
    return switch (statut) {
      'EnAttenteDepot' => (AppTheme.accent, 'En attente depot'),
      'DeposeParClient' => (AppTheme.accent, 'Depose par client'),
      'ArriveDansPaysDest' => (AppTheme.primary, 'Arrive destination'),
      'ReceptionneParPointRelais' => (AppTheme.primary, 'Receptionne'),
      'DisponibleAuRetrait' => (AppTheme.success, 'Disponible au retrait'),
      'RetireParDestinataire' => (AppTheme.success, 'Retire'),
      'LivraisonCloturee' => (AppTheme.success, 'Cloture'),
      'Incident' => (AppTheme.danger, 'Incident'),
      'Refuse' => (AppTheme.danger, 'Refuse'),
      'Endommage' => (AppTheme.danger, 'Endommage'),
      'Perdu' => (AppTheme.danger, 'Perdu'),
      _ => (AppTheme.textMuted, statut),
    };
  }
}
