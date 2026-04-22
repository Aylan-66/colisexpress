import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class CreateTrajetScreen extends StatefulWidget {
  const CreateTrajetScreen({super.key});

  @override
  State<CreateTrajetScreen> createState() => _CreateTrajetScreenState();
}

class _CreateTrajetScreenState extends State<CreateTrajetScreen> {
  final _villeDepartCtrl = TextEditingController();
  final _villeArriveeCtrl = TextEditingController();
  final _poidsMaxCtrl = TextEditingController(text: '500');
  final _nbColisCtrl = TextEditingController(text: '30');
  final _prixCtrl = TextEditingController();
  final _conditionsCtrl = TextEditingController();

  String _paysDepart = 'France';
  String _paysArrivee = 'Algérie';
  String _modeTarif = 'PrixParColis';
  DateTime _dateDepart = DateTime.now().add(const Duration(days: 3));
  DateTime _dateArrivee = DateTime.now().add(const Duration(days: 6));
  bool _loading = false;
  String? _error;

  // Étapes inline
  final List<Map<String, dynamic>> _etapes = [];
  Map<String, dynamic>? _relaisDepart;
  Map<String, dynamic>? _relaisArrivee;
  TimeOfDay _heureDepart = const TimeOfDay(hour: 8, minute: 0);
  TimeOfDay _heureArrivee = const TimeOfDay(hour: 16, minute: 0);

  Future<void> _pickDate(bool isDepart) async {
    final picked = await showDatePicker(
      context: context, initialDate: isDepart ? _dateDepart : _dateArrivee,
      firstDate: DateTime.now(), lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      setState(() {
        if (isDepart) { _dateDepart = picked; if (_dateArrivee.isBefore(_dateDepart)) _dateArrivee = _dateDepart.add(const Duration(days: 3)); }
        else _dateArrivee = picked;
      });
    }
  }

  Future<Map<String, dynamic>?> _selectRelais({String? label}) async {
    final api = context.read<ApiService>();
    final relais = await api.getRelaisDisponibles();
    if (!mounted || relais.isEmpty) { setState(() => _error = 'Aucun point relais disponible.'); return null; }

    return showModalBottomSheet<Map<String, dynamic>>(
      context: context, isScrollControlled: true,
      builder: (ctx) => _RelaisPickerSheet(relaisList: relais, label: label ?? 'Sélectionner un relais'),
    );
  }

  Future<void> _addEtapeIntermediaire() async {
    final relais = await _selectRelais(label: 'Ajouter un arrêt intermédiaire');
    if (relais == null || !mounted) return;

    // Date min = date de la dernière étape intermédiaire ou date départ
    final prevDate = _etapes.isNotEmpty ? (_etapes.last['date'] as DateTime? ?? _dateDepart) : _dateDepart;
    final prevTime = _etapes.isNotEmpty ? (_etapes.last['heure'] as TimeOfDay? ?? const TimeOfDay(hour: 10, minute: 0)) : _heureDepart;

    final date = await showDatePicker(
      context: context, initialDate: prevDate,
      firstDate: prevDate, lastDate: _dateArrivee.add(const Duration(days: 30)),
    );
    if (date == null || !mounted) return;

    final time = await showTimePicker(context: context, initialTime: prevTime);
    if (time == null) return;

    setState(() {
      _etapes.add({
        'relais': relais,
        'date': date,
        'heure': TimeOfDay(hour: time.hour, minute: time.minute),
      });
    });
  }

  Future<void> _submit() async {
    if (_villeDepartCtrl.text.isEmpty || _villeArriveeCtrl.text.isEmpty) {
      setState(() => _error = 'Remplissez les villes de départ et d\'arrivée.'); return;
    }
    if (_relaisDepart == null) { setState(() => _error = 'Sélectionnez un relais de départ.'); return; }
    if (_relaisArrivee == null) { setState(() => _error = 'Sélectionnez un relais d\'arrivée.'); return; }

    setState(() { _loading = true; _error = null; });
    final api = context.read<ApiService>();

    final data = {
      'paysDepart': _paysDepart,
      'villeDepart': _villeDepartCtrl.text.trim(),
      'paysArrivee': _paysArrivee,
      'villeArrivee': _villeArriveeCtrl.text.trim(),
      'dateDepart': _dateDepart.toUtc().toIso8601String(),
      'dateEstimeeArrivee': _dateArrivee.toUtc().toIso8601String(),
      'capaciteMaxPoids': double.tryParse(_poidsMaxCtrl.text) ?? 500,
      'nombreMaxColis': int.tryParse(_nbColisCtrl.text) ?? 30,
      'modeTarification': _modeTarif == 'PrixParColis' ? 0 : (_modeTarif == 'PrixAuKilo' ? 1 : 2),
      if (_modeTarif == 'PrixParColis' || _modeTarif == 'Forfait') 'prixParColis': double.tryParse(_prixCtrl.text) ?? 0,
      if (_modeTarif == 'PrixAuKilo' || _modeTarif == 'Forfait') 'prixAuKilo': double.tryParse(_prixCtrl.text) ?? 0,
      'conditions': _conditionsCtrl.text.trim(),
    };

    final res = await api.createTrajet(data);
    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; }); return;
    }

    final trajetId = res['id'];

    // Ajouter étape départ
    final depDt = DateTime(_dateDepart.year, _dateDepart.month, _dateDepart.day, _heureDepart.hour, _heureDepart.minute);
    await api.addEtape(trajetId, _relaisDepart!['id'], depDt.toUtc().toIso8601String());

    // Ajouter étapes intermédiaires
    for (final e in _etapes) {
      final h = e['heure'] as TimeOfDay;
      final d = e['date'] as DateTime? ?? _dateDepart;
      final dt = DateTime(d.year, d.month, d.day, h.hour, h.minute);
      await api.addEtape(trajetId, e['relais']['id'], dt.toUtc().toIso8601String());
    }

    // Ajouter étape arrivée
    final arrDt = DateTime(_dateArrivee.year, _dateArrivee.month, _dateArrivee.day, _heureArrivee.hour, _heureArrivee.minute);
    await api.addEtape(trajetId, _relaisArrivee!['id'], arrDt.toUtc().toIso8601String());

    setState(() => _loading = false);
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Trajet créé avec ses étapes !'), backgroundColor: Colors.green));
      Navigator.pop(context, true);
    }
  }

  String _fmt(DateTime d) => '${d.day}/${d.month}/${d.year}';

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Nouveau trajet')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (_error != null)
              Container(
                width: double.infinity, padding: const EdgeInsets.all(12), margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(color: AppTheme.danger.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(8)),
                child: Text(_error!, style: const TextStyle(color: AppTheme.danger, fontSize: 13)),
              ),

            _label('TRAJET'),
            DropdownButtonFormField<String>(
              initialValue: _paysDepart,
              decoration: const InputDecoration(labelText: 'PAYS DÉPART'),
              items: ['France', 'Espagne', 'Algérie', 'Maroc', 'Tunisie'].map((p) => DropdownMenuItem(value: p, child: Text(p))).toList(),
              onChanged: (v) => setState(() => _paysDepart = v!),
            ),
            const SizedBox(height: 10),
            TextField(controller: _villeDepartCtrl, decoration: const InputDecoration(labelText: 'VILLE DÉPART', hintText: 'Paris')),
            const SizedBox(height: 10),
            DropdownButtonFormField<String>(
              initialValue: _paysArrivee,
              decoration: const InputDecoration(labelText: 'PAYS ARRIVÉE'),
              items: ['France', 'Espagne', 'Algérie', 'Maroc', 'Tunisie'].map((p) => DropdownMenuItem(value: p, child: Text(p))).toList(),
              onChanged: (v) => setState(() => _paysArrivee = v!),
            ),
            const SizedBox(height: 10),
            TextField(controller: _villeArriveeCtrl, decoration: const InputDecoration(labelText: 'VILLE ARRIVÉE', hintText: 'Alger')),

            const SizedBox(height: 20), _label('DATES'),
            Row(children: [
              Expanded(child: OutlinedButton.icon(icon: const Icon(Icons.calendar_today, size: 16), label: Text('Départ: ${_fmt(_dateDepart)}'), onPressed: () => _pickDate(true))),
              const SizedBox(width: 10),
              Expanded(child: OutlinedButton.icon(icon: const Icon(Icons.calendar_today, size: 16), label: Text('Arrivée: ${_fmt(_dateArrivee)}'), onPressed: () => _pickDate(false))),
            ]),

            // ============================================
            // ÉTAPES (RELAIS DÉPART + INTERMÉDIAIRES + ARRIVÉE)
            // ============================================
            const SizedBox(height: 20), _label('ÉTAPES DE LA TOURNÉE'),
            const Text('Minimum : 1 relais départ + 1 relais arrivée', style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
            const SizedBox(height: 10),

            // Relais départ + heure
            _relaisSelector('Relais de départ', _relaisDepart, () async {
              final r = await _selectRelais(label: 'Relais de départ');
              if (r != null) setState(() => _relaisDepart = r);
            }),
            if (_relaisDepart != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: OutlinedButton.icon(
                  icon: const Icon(Icons.schedule, size: 16),
                  label: Text('Départ : ${_fmt(_dateDepart)} à ${_heureDepart.hour.toString().padLeft(2, '0')}:${_heureDepart.minute.toString().padLeft(2, '0')}'),
                  onPressed: () async {
                    final t = await showTimePicker(context: context, initialTime: _heureDepart, helpText: 'Heure de départ');
                    if (t != null) setState(() => _heureDepart = t);
                  },
                ),
              ),

            // Étapes intermédiaires
            ..._etapes.asMap().entries.map((entry) {
              final i = entry.key;
              final e = entry.value;
              final r = e['relais'] as Map<String, dynamic>;
              final h = e['heure'] as TimeOfDay;
              final d = e['date'] as DateTime? ?? _dateDepart;
              return Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: Column(
                  children: [
                    ListTile(
                      leading: const Icon(Icons.swap_vert, color: AppTheme.accent),
                      title: Text(r['nomRelais'] ?? '—', style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
                      subtitle: Text('${r['ville']}, ${r['pays']}', style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                      trailing: IconButton(icon: const Icon(Icons.delete_outline, color: AppTheme.danger, size: 20), onPressed: () => setState(() => _etapes.removeAt(i))),
                    ),
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.schedule, size: 16),
                        label: Text('Arrivée : ${d.day}/${d.month}/${d.year} à ${h.hour.toString().padLeft(2, '0')}:${h.minute.toString().padLeft(2, '0')}'),
                        onPressed: () async {
                          final pickedDate = await showDatePicker(
                            context: context, initialDate: d,
                            firstDate: _dateDepart, lastDate: _dateArrivee.add(const Duration(days: 30)),
                          );
                          if (pickedDate == null || !mounted) return;
                          final pickedTime = await showTimePicker(context: context, initialTime: h);
                          if (pickedTime == null || !mounted) return;
                          setState(() {
                            _etapes[i]['date'] = pickedDate;
                            _etapes[i]['heure'] = pickedTime;
                          });
                        },
                      ),
                    ),
                  ],
                ),
              );
            }),

            SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                icon: const Icon(Icons.add_location_alt, size: 18, color: AppTheme.accent),
                label: const Text('Ajouter un arrêt intermédiaire'),
                onPressed: _addEtapeIntermediaire,
              ),
            ),
            const SizedBox(height: 8),

            // Relais arrivée + heure
            _relaisSelector('Relais d\'arrivée (destination)', _relaisArrivee, () async {
              final r = await _selectRelais(label: 'Relais d\'arrivée');
              if (r != null) setState(() => _relaisArrivee = r);
            }),
            if (_relaisArrivee != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: OutlinedButton.icon(
                  icon: const Icon(Icons.schedule, size: 16),
                  label: Text('Arrivée : ${_fmt(_dateArrivee)} à ${_heureArrivee.hour.toString().padLeft(2, '0')}:${_heureArrivee.minute.toString().padLeft(2, '0')}'),
                  onPressed: () async {
                    final t = await showTimePicker(context: context, initialTime: _heureArrivee, helpText: 'Heure d\'arrivée');
                    if (t != null) setState(() => _heureArrivee = t);
                  },
                ),
              ),

            const SizedBox(height: 20), _label('CAPACITÉ'),
            Row(children: [
              Expanded(child: TextField(controller: _poidsMaxCtrl, decoration: const InputDecoration(labelText: 'POIDS MAX (KG)'), keyboardType: TextInputType.number)),
              const SizedBox(width: 10),
              Expanded(child: TextField(controller: _nbColisCtrl, decoration: const InputDecoration(labelText: 'NB COLIS MAX'), keyboardType: TextInputType.number)),
            ]),

            const SizedBox(height: 20), _label('TARIFICATION'),
            DropdownButtonFormField<String>(
              initialValue: _modeTarif,
              decoration: const InputDecoration(labelText: 'MODE'),
              items: const [
                DropdownMenuItem(value: 'PrixParColis', child: Text('Prix par colis')),
                DropdownMenuItem(value: 'PrixAuKilo', child: Text('Prix au kilo')),
                DropdownMenuItem(value: 'Forfait', child: Text('Forfait + kilo')),
              ],
              onChanged: (v) => setState(() => _modeTarif = v!),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: _prixCtrl,
              decoration: InputDecoration(labelText: _modeTarif == 'PrixAuKilo' ? 'PRIX AU KILO (€)' : 'PRIX PAR COLIS (€)', hintText: _modeTarif == 'PrixAuKilo' ? '7' : '85'),
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
            ),
            const SizedBox(height: 10),
            TextField(controller: _conditionsCtrl, decoration: const InputDecoration(labelText: 'CONDITIONS (OPTIONNEL)'), maxLines: 2),

            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _loading ? null : _submit,
                style: ElevatedButton.styleFrom(backgroundColor: AppTheme.accent, padding: const EdgeInsets.symmetric(vertical: 14)),
                child: _loading
                    ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Text('Publier le trajet'),
              ),
            ),
            const SizedBox(height: 32),
          ],
        ),
      ),
    );
  }

  Widget _label(String text) => Padding(
    padding: const EdgeInsets.only(bottom: 10),
    child: Text(text, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
  );

  Widget _relaisSelector(String label, Map<String, dynamic>? selected, VoidCallback onTap) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        onTap: onTap,
        leading: Icon(selected != null ? Icons.check_circle : Icons.add_circle_outline,
            color: selected != null ? AppTheme.success : AppTheme.primary),
        title: Text(selected != null ? (selected['nomRelais'] ?? '—') : label,
            style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13, color: selected != null ? AppTheme.textDark : AppTheme.primary)),
        subtitle: selected != null
            ? Text('${selected['ville']}, ${selected['pays']}', style: const TextStyle(fontSize: 12, color: AppTheme.textMuted))
            : const Text('Appuyez pour sélectionner', style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
        trailing: selected != null
            ? IconButton(icon: const Icon(Icons.close, size: 18), onPressed: () => setState(() { if (label.contains('départ')) _relaisDepart = null; else _relaisArrivee = null; }))
            : const Icon(Icons.chevron_right, color: AppTheme.textMuted),
      ),
    );
  }
}

class _RelaisPickerSheet extends StatefulWidget {
  final List<dynamic> relaisList;
  final String label;
  const _RelaisPickerSheet({required this.relaisList, required this.label});

  @override
  State<_RelaisPickerSheet> createState() => _RelaisPickerSheetState();
}

class _RelaisPickerSheetState extends State<_RelaisPickerSheet> {
  String _search = '';

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
      initialChildSize: 0.8, maxChildSize: 0.95, minChildSize: 0.5, expand: false,
      builder: (ctx, scrollCtrl) => Container(
        decoration: const BoxDecoration(color: AppTheme.bg, borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
        child: Column(
          children: [
            Container(width: 40, height: 4, margin: const EdgeInsets.symmetric(vertical: 12),
                decoration: BoxDecoration(color: AppTheme.border, borderRadius: BorderRadius.circular(2))),
            Padding(padding: const EdgeInsets.symmetric(horizontal: 20), child: Text(widget.label, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800))),
            const SizedBox(height: 10),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: TextField(
                decoration: const InputDecoration(labelText: 'RECHERCHER', hintText: 'Ville, département, région...', prefixIcon: Icon(Icons.search, size: 20)),
                onChanged: (v) => setState(() => _search = v),
              ),
            ),
            const SizedBox(height: 8),
            Expanded(
              child: ListView.builder(
                controller: scrollCtrl, padding: const EdgeInsets.symmetric(horizontal: 16),
                itemCount: _filtered.length,
                itemBuilder: (ctx, i) {
                  final r = _filtered[i] as Map<String, dynamic>;
                  final horaires = r['heureOuverture'] != null ? '${r['heureOuverture']} — ${r['heureFermeture']}' : 'Horaires non définis';
                  return Card(
                    child: ListTile(
                      onTap: () => Navigator.pop(context, r),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                      leading: const Icon(Icons.store, color: AppTheme.primary),
                      title: Text(r['nomRelais'] ?? '—', style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
                      subtitle: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('${r['ville']}, ${r['departement'] ?? ''} — ${r['pays']}', style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                          Text(horaires, style: const TextStyle(fontSize: 11, color: AppTheme.textMuted)),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
