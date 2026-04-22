import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'etape_detail_screen.dart';

class EtapesScreen extends StatefulWidget {
  final String trajetId;
  final String trajetLabel;

  const EtapesScreen({super.key, required this.trajetId, required this.trajetLabel});

  @override
  State<EtapesScreen> createState() => _EtapesScreenState();
}

class _EtapesScreenState extends State<EtapesScreen> {
  List<dynamic> _etapes = [];
  bool _loading = true;
  String? _error;
  String? _success;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    _etapes = await context.read<ApiService>().getEtapesTrajet(widget.trajetId);
    setState(() => _loading = false);
  }

  Future<void> _addEtape() async {
    final api = context.read<ApiService>();
    final relaisList = await api.getRelaisDisponibles();

    if (!mounted) return;
    if (relaisList.isEmpty) {
      setState(() => _error = 'Aucun point relais disponible.');
      return;
    }

    // Calculer la date/heure minimum : dernière étape existante ou début du trajet
    DateTime minDate;
    TimeOfDay minTime;
    if (_etapes.isNotEmpty) {
      final lastEtape = _etapes.last;
      final lastDt = DateTime.tryParse(lastEtape['heureEstimeeArrivee']?.toString() ?? '')?.toLocal();
      minDate = lastDt ?? DateTime.now();
      minTime = lastDt != null ? TimeOfDay(hour: lastDt.hour, minute: lastDt.minute) : const TimeOfDay(hour: 8, minute: 0);
    } else {
      minDate = DateTime.now().add(const Duration(days: 1));
      minTime = const TimeOfDay(hour: 8, minute: 0);
    }

    final result = await showModalBottomSheet<Map<String, dynamic>>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => _SelectRelaisSheet(relaisList: relaisList, minDate: minDate, minTime: minTime),
    );

    if (result == null || !mounted) return;

    setState(() { _loading = true; _error = null; _success = null; });
    final res = await api.addEtape(
      widget.trajetId,
      result['relaisId'],
      result['heureEstimee'],
    );

    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; });
    } else {
      final warning = res['warning'];
      setState(() {
        _success = warning ?? 'Étape ajoutée : ${res['relaisNom']}';
        if (warning != null) _error = warning;
      });
      await _load();
    }
  }

  Future<void> _removeEtape(String etapeId) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Supprimer cette étape ?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          TextButton(onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Supprimer', style: TextStyle(color: AppTheme.danger))),
        ],
      ),
    );
    if (confirm != true || !mounted) return;
    await context.read<ApiService>().removeEtape(widget.trajetId, etapeId);
    _load();
  }

  Future<void> _lancerTournee() async {
    setState(() { _loading = true; _error = null; _success = null; });
    final res = await context.read<ApiService>().lancerTournee(widget.trajetId);
    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; });
    } else {
      setState(() => _success = 'Tournée lancée !');
      await _load();
    }
  }

  Future<void> _demanderAnnulation() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Demander l\'annulation ?'),
        content: const Text('Cette action enverra une demande à l\'admin. Les clients Stripe seront remboursés. Vous ne pourrez pas annuler vous-même.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Non')),
          TextButton(onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Oui, annuler', style: TextStyle(color: AppTheme.danger))),
        ],
      ),
    );
    if (confirm != true || !mounted) return;

    final res = await context.read<ApiService>().demanderAnnulation(widget.trajetId);
    if (res.containsKey('error')) {
      setState(() => _error = res['error']);
    } else {
      setState(() => _success = res['message'] ?? 'Demande d\'annulation envoyée.');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Demande d\'annulation envoyée à l\'admin.'), backgroundColor: Colors.orange),
        );
      }
    }
  }

  Future<void> _modifierHeureEtape(String etapeId) async {
    final time = await showTimePicker(context: context, initialTime: const TimeOfDay(hour: 12, minute: 0), helpText: 'Nouvelle heure d\'arrivée');
    if (time == null || !mounted) return;

    final date = await showDatePicker(context: context, initialDate: DateTime.now().add(const Duration(days: 3)),
        firstDate: DateTime.now(), lastDate: DateTime.now().add(const Duration(days: 365)), helpText: 'Date d\'arrivée');
    if (date == null || !mounted) return;

    final dt = DateTime(date.year, date.month, date.day, time.hour, time.minute);
    setState(() { _loading = true; _error = null; });
    final res = await context.read<ApiService>().updateEtapeHeure(widget.trajetId, etapeId, dt.toUtc().toIso8601String());

    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; });
    } else {
      final warning = res['warning'];
      setState(() { _success = warning ?? 'Heure mise à jour.'; });
      if (warning != null) setState(() => _error = warning);
      await _load();
    }
  }

  void _ouvrirMaps(Map<String, dynamic> relais) {
    final adresse = '${relais['nomRelais'] ?? ''}, ${relais['ville'] ?? ''}, ${relais['pays'] ?? ''}';
    final encoded = Uri.encodeComponent(adresse);
    // Essayer Waze d'abord, sinon Google Maps
    launchUrl(Uri.parse('https://waze.com/ul?q=$encoded'), mode: LaunchMode.externalApplication);
  }

  Future<void> _marquerArrivee(String etapeId) async {
    setState(() { _loading = true; _error = null; });
    final res = await context.read<ApiService>().marquerArrivee(widget.trajetId, etapeId);
    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; });
    } else {
      setState(() => _success = res['message'] ?? 'Arrivée confirmée.');
      await _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Fiche de tournée'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add_location_alt, color: AppTheme.accent),
            onPressed: _addEtape,
            tooltip: 'Ajouter une étape',
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  // Header
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(widget.trajetLabel,
                              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                          const SizedBox(height: 6),
                          Text('${_etapes.length} étape(s) planifiée(s)',
                              style: const TextStyle(color: AppTheme.textMuted, fontSize: 13)),
                        ],
                      ),
                    ),
                  ),

                  if (_success != null) _alert(_success!, AppTheme.success),
                  if (_error != null) _alert(_error!, AppTheme.danger),

                  const SizedBox(height: 12),

                  // Étapes
                  if (_etapes.isEmpty)
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(32),
                        child: Column(
                          children: [
                            const Icon(Icons.route, size: 48, color: AppTheme.textMuted),
                            const SizedBox(height: 12),
                            const Text('Aucune étape', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
                            const SizedBox(height: 4),
                            const Text('Ajoutez des points relais à votre tournée.',
                                style: TextStyle(color: AppTheme.textMuted, fontSize: 13)),
                            const SizedBox(height: 16),
                            ElevatedButton.icon(
                              icon: const Icon(Icons.add_location_alt, size: 18),
                              label: const Text('Ajouter un relais'),
                              style: ElevatedButton.styleFrom(backgroundColor: AppTheme.accent),
                              onPressed: _addEtape,
                            ),
                          ],
                        ),
                      ),
                    )
                  else
                    ..._etapes.asMap().entries.map((entry) {
                      final i = entry.key;
                      final e = entry.value;
                      return _etapeCard(e, i == _etapes.length - 1);
                    }),

                  if (_etapes.isNotEmpty) ...[
                    const SizedBox(height: 16),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        icon: const Icon(Icons.play_arrow, size: 20),
                        label: const Text('Lancer la tournée'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppTheme.success,
                          padding: const EdgeInsets.symmetric(vertical: 14),
                        ),
                        onPressed: _lancerTournee,
                      ),
                    ),
                    const SizedBox(height: 10),
                    SizedBox(
                      width: double.infinity,
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.cancel_outlined, size: 18, color: AppTheme.danger),
                        label: const Text('Demander l\'annulation', style: TextStyle(color: AppTheme.danger)),
                        style: OutlinedButton.styleFrom(side: const BorderSide(color: AppTheme.danger)),
                        onPressed: _demanderAnnulation,
                      ),
                    ),
                  ],
                ],
              ),
            ),
    );
  }

  Widget _etapeCard(Map<String, dynamic> e, bool isLast) {
    final relais = e['relais'] as Map<String, dynamic>? ?? {};
    final statut = e['statut']?.toString() ?? 'Planifiee';
    final ouvert = e['relaisOuvertALArrivee'] == true;
    final heureEstimee = DateTime.tryParse(e['heureEstimeeArrivee']?.toString() ?? '')?.toLocal();
    final heureReelle = DateTime.tryParse(e['heureReelleArrivee']?.toString() ?? '')?.toLocal();
    final heureStr = heureEstimee != null ? '${heureEstimee.hour}:${heureEstimee.minute.toString().padLeft(2, '0')}' : '—';

    Color statutColor;
    String statutLabel;
    switch (statut) {
      case 'Terminee': statutColor = AppTheme.success; statutLabel = 'Terminé'; break;
      case 'EnCours': statutColor = AppTheme.accent; statutLabel = 'En cours'; break;
      default: statutColor = AppTheme.textMuted; statutLabel = 'Planifié';
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Column(
        children: [
          // Header avec nom + statut + horaire
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: statutColor.withValues(alpha: 0.05),
              borderRadius: const BorderRadius.vertical(top: Radius.circular(12)),
            ),
            child: Row(
              children: [
                Container(
                  width: 40, height: 40,
                  decoration: BoxDecoration(color: statutColor.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(10)),
                  child: Center(child: Text('${e['ordre']}', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16, color: statutColor))),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(relais['nomRelais'] ?? '—', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
                      Text('${relais['ville'] ?? ''}, ${relais['pays'] ?? ''}', style: const TextStyle(color: AppTheme.textMuted, fontSize: 13)),
                    ],
                  ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(color: statutColor.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(8)),
                      child: Text(statutLabel, style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: statutColor)),
                    ),
                    const SizedBox(height: 4),
                    Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(ouvert ? Icons.lock_open : Icons.lock, size: 14, color: ouvert ? AppTheme.success : AppTheme.danger),
                        const SizedBox(width: 3),
                        Text(ouvert ? 'Ouvert' : 'Fermé', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: ouvert ? AppTheme.success : AppTheme.danger)),
                      ],
                    ),
                  ],
                ),
              ],
            ),
          ),

          // Horaire + arrivée réelle
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                const Icon(Icons.schedule, size: 18, color: AppTheme.textMuted),
                const SizedBox(width: 8),
                Text('Arrivée prévue : $heureStr', style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
                if (heureReelle != null) ...[
                  const SizedBox(width: 12),
                  Icon(Icons.check, size: 16, color: AppTheme.success),
                  Text(' Réelle : ${heureReelle.hour}:${heureReelle.minute.toString().padLeft(2, '0')}',
                      style: const TextStyle(fontSize: 13, color: AppTheme.success, fontWeight: FontWeight.w600)),
                ],
              ],
            ),
          ),

          // Boutons d'action — gros et clairs
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
            child: Row(
              children: [
                // Voir les colis
                Expanded(
                  child: OutlinedButton.icon(
                    icon: const Icon(Icons.inventory_2, size: 18),
                    label: const Text('Voir colis'),
                    style: OutlinedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 12)),
                    onPressed: () => Navigator.push(context, MaterialPageRoute(
                      builder: (_) => EtapeDetailScreen(
                        trajetId: widget.trajetId, etapeId: e['id'],
                        relaisNom: relais['nomRelais'] ?? '—',
                      ),
                    )),
                  ),
                ),
                const SizedBox(width: 8),

                if (statut == 'Planifiee') ...[
                  // Modifier l'heure
                  Expanded(
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.edit, size: 18, color: AppTheme.primary),
                      label: const Text('Modifier heure'),
                      style: OutlinedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 12)),
                      onPressed: () => _modifierHeureEtape(e['id']),
                    ),
                  ),
                  const SizedBox(width: 8),
                  // Supprimer
                  SizedBox(
                    width: 48,
                    child: IconButton(
                      icon: const Icon(Icons.delete, color: AppTheme.danger, size: 24),
                      onPressed: () => _removeEtape(e['id']),
                    ),
                  ),
                ],

                if (statut == 'EnCours') ...[
                  // Navigation Maps
                  Expanded(
                    child: ElevatedButton.icon(
                      icon: const Icon(Icons.navigation, size: 18),
                      label: const Text('Naviguer'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppTheme.primary,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                      ),
                      onPressed: () => _ouvrirMaps(relais),
                    ),
                  ),
                  const SizedBox(width: 8),
                  // Marquer arrivée
                  Expanded(
                    child: ElevatedButton.icon(
                      icon: const Icon(Icons.check_circle, size: 18),
                      label: const Text('Arrivé'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppTheme.success,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                      ),
                      onPressed: () => _marquerArrivee(e['id']),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _alert(String msg, Color color) => Container(
        width: double.infinity,
        padding: const EdgeInsets.all(12),
        margin: const EdgeInsets.only(top: 10),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Text(msg, style: TextStyle(color: color, fontWeight: FontWeight.w600, fontSize: 13)),
      );
}

// ============================================
// Bottom sheet pour sélectionner un relais + heure
// ============================================

class _SelectRelaisSheet extends StatefulWidget {
  final List<dynamic> relaisList;
  final DateTime minDate;
  final TimeOfDay minTime;
  const _SelectRelaisSheet({required this.relaisList, required this.minDate, required this.minTime});

  @override
  State<_SelectRelaisSheet> createState() => _SelectRelaisSheetState();
}

class _SelectRelaisSheetState extends State<_SelectRelaisSheet> {
  Map<String, dynamic>? _selected;
  late TimeOfDay _heure;
  late DateTime _date;
  String _search = '';

  @override
  void initState() {
    super.initState();
    _date = widget.minDate;
    _heure = widget.minTime;
  }

  List<dynamic> get _filtered {
    if (_search.isEmpty) return widget.relaisList;
    final q = _search.toLowerCase();
    return widget.relaisList.where((r) {
      final nom = (r['nomRelais'] ?? '').toString().toLowerCase();
      final ville = (r['ville'] ?? '').toString().toLowerCase();
      final dept = (r['departement'] ?? '').toString().toLowerCase();
      final region = (r['region'] ?? '').toString().toLowerCase();
      final pays = (r['pays'] ?? '').toString().toLowerCase();
      return nom.contains(q) || ville.contains(q) || dept.contains(q) || region.contains(q) || pays.contains(q);
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.85,
      maxChildSize: 0.95,
      minChildSize: 0.5,
      expand: false,
      builder: (ctx, scrollCtrl) => Container(
        decoration: const BoxDecoration(
          color: AppTheme.bg,
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        child: Column(
          children: [
            Container(
              width: 40, height: 4,
              margin: const EdgeInsets.symmetric(vertical: 12),
              decoration: BoxDecoration(color: AppTheme.border, borderRadius: BorderRadius.circular(2)),
            ),
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: 20),
              child: Text('Ajouter une étape', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
            ),
            const SizedBox(height: 12),

            // Date + heure
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.calendar_today, size: 16),
                      label: Text('${_date.day}/${_date.month}/${_date.year}'),
                      onPressed: () async {
                        final d = await showDatePicker(
                          context: context, initialDate: _date,
                          firstDate: widget.minDate, lastDate: widget.minDate.add(const Duration(days: 365)),
                        );
                        if (d != null) setState(() => _date = d);
                      },
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.schedule, size: 16),
                      label: Text('${_heure.hour}:${_heure.minute.toString().padLeft(2, '0')}'),
                      onPressed: () async {
                        final t = await showTimePicker(context: context, initialTime: _heure);
                        if (t != null) setState(() => _heure = t);
                      },
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 10),

            // Recherche
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: TextField(
                decoration: const InputDecoration(
                  labelText: 'RECHERCHER', hintText: 'Ville, département, région...',
                  prefixIcon: Icon(Icons.search, size: 20),
                ),
                onChanged: (v) => setState(() => _search = v),
              ),
            ),
            const SizedBox(height: 8),

            // Liste des relais filtrée
            Expanded(
              child: ListView.builder(
                controller: scrollCtrl,
                padding: const EdgeInsets.symmetric(horizontal: 16),
                itemCount: _filtered.length,
                itemBuilder: (ctx, i) {
                  final r = _filtered[i] as Map<String, dynamic>;
                  final isSelected = _selected?['id'] == r['id'];
                  final horaires = r['heureOuverture'] != null
                      ? '${r['heureOuverture']} — ${r['heureFermeture']}'
                      : 'Horaires non définis';

                  return Card(
                    color: isSelected ? AppTheme.primary.withValues(alpha: 0.05) : null,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10),
                      side: BorderSide(
                        color: isSelected ? AppTheme.primary : AppTheme.border,
                        width: isSelected ? 2 : 1,
                      ),
                    ),
                    child: ListTile(
                      onTap: () => setState(() => _selected = r),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                      leading: Icon(Icons.store,
                          color: isSelected ? AppTheme.primary : AppTheme.textMuted),
                      title: Text(r['nomRelais'] ?? '—',
                          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
                      subtitle: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('${r['ville']}, ${r['departement'] ?? ''} — ${r['pays']}',
                              style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                          Text(horaires,
                              style: const TextStyle(fontSize: 11, color: AppTheme.textMuted)),
                          if ((r['joursOuverture'] ?? '').isNotEmpty)
                            Text(r['joursOuverture'],
                                style: const TextStyle(fontSize: 10, color: AppTheme.textMuted)),
                        ],
                      ),
                      trailing: isSelected
                          ? const Icon(Icons.check_circle, color: AppTheme.primary)
                          : null,
                    ),
                  );
                },
              ),
            ),

            // Bouton confirmer
            Padding(
              padding: const EdgeInsets.all(20),
              child: SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _selected == null
                      ? null
                      : () {
                          final dt = DateTime(
                            _date.year, _date.month, _date.day,
                            _heure.hour, _heure.minute,
                          );
                          Navigator.pop(context, {
                            'relaisId': _selected!['id'],
                            'heureEstimee': dt.toUtc().toIso8601String(),
                          });
                        },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.accent,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                  child: Text(_selected == null
                      ? 'Sélectionnez un relais'
                      : 'Ajouter ${_selected!['nomRelais']}'),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
