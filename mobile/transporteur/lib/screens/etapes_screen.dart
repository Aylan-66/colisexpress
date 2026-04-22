import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

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

    final result = await showModalBottomSheet<Map<String, dynamic>>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => _SelectRelaisSheet(relaisList: relaisList),
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
    final heureEstimee = DateTime.tryParse(e['heureEstimeeArrivee']?.toString() ?? '');
    final heureReelle = DateTime.tryParse(e['heureReelleArrivee']?.toString() ?? '');

    Color statutColor;
    IconData statutIcon;
    switch (statut) {
      case 'Terminee': statutColor = AppTheme.success; statutIcon = Icons.check_circle; break;
      case 'EnCours': statutColor = AppTheme.accent; statutIcon = Icons.directions; break;
      default: statutColor = AppTheme.textMuted; statutIcon = Icons.circle_outlined;
    }

    return Column(
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              children: [
                // Timeline dot
                Column(
                  children: [
                    Icon(statutIcon, color: statutColor, size: 24),
                    if (!isLast) Container(width: 2, height: 30, color: AppTheme.border),
                  ],
                ),
                const SizedBox(width: 14),

                // Info
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('${e['ordre']}. ${relais['nomRelais'] ?? '—'}',
                          style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14)),
                      Text('${relais['ville']}, ${relais['pays']}',
                          style: const TextStyle(color: AppTheme.textMuted, fontSize: 12)),
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          Icon(Icons.schedule, size: 14, color: AppTheme.textMuted),
                          const SizedBox(width: 4),
                          Text(
                            heureEstimee != null
                                ? '${heureEstimee.hour}:${heureEstimee.minute.toString().padLeft(2, '0')}'
                                : '—',
                            style: const TextStyle(fontSize: 12, color: AppTheme.textMuted),
                          ),
                          const SizedBox(width: 10),
                          Icon(
                            ouvert ? Icons.lock_open : Icons.lock,
                            size: 14,
                            color: ouvert ? AppTheme.success : AppTheme.danger,
                          ),
                          const SizedBox(width: 4),
                          Text(
                            ouvert ? 'Ouvert' : 'Fermé',
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                              color: ouvert ? AppTheme.success : AppTheme.danger,
                            ),
                          ),
                        ],
                      ),
                      if (heureReelle != null)
                        Padding(
                          padding: const EdgeInsets.only(top: 4),
                          child: Text(
                            'Arrivé à ${heureReelle.hour}:${heureReelle.minute.toString().padLeft(2, '0')}',
                            style: const TextStyle(fontSize: 11, color: AppTheme.success, fontWeight: FontWeight.w600),
                          ),
                        ),
                    ],
                  ),
                ),

                // Actions
                if (statut == 'Planifiee')
                  IconButton(
                    icon: const Icon(Icons.delete_outline, size: 20, color: AppTheme.danger),
                    onPressed: () => _removeEtape(e['id']),
                  ),
                if (statut == 'EnCours')
                  ElevatedButton(
                    onPressed: () => _marquerArrivee(e['id']),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppTheme.success,
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    ),
                    child: const Text('Arrivé', style: TextStyle(fontSize: 12)),
                  ),
              ],
            ),
          ),
        ),
      ],
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
  const _SelectRelaisSheet({required this.relaisList});

  @override
  State<_SelectRelaisSheet> createState() => _SelectRelaisSheetState();
}

class _SelectRelaisSheetState extends State<_SelectRelaisSheet> {
  Map<String, dynamic>? _selected;
  TimeOfDay _heure = const TimeOfDay(hour: 10, minute: 0);
  DateTime _date = DateTime.now().add(const Duration(days: 3));

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
                          context: context,
                          initialDate: _date,
                          firstDate: DateTime.now(),
                          lastDate: DateTime.now().add(const Duration(days: 365)),
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

            const SizedBox(height: 12),
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: 20),
              child: Text('Sélectionnez un point relais', style: TextStyle(fontSize: 13, color: AppTheme.textMuted)),
            ),
            const SizedBox(height: 8),

            // Liste des relais
            Expanded(
              child: ListView.builder(
                controller: scrollCtrl,
                padding: const EdgeInsets.symmetric(horizontal: 16),
                itemCount: widget.relaisList.length,
                itemBuilder: (ctx, i) {
                  final r = widget.relaisList[i] as Map<String, dynamic>;
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
                          Text('${r['ville']}, ${r['pays']}',
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
