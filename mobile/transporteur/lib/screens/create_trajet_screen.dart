import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'tarifs_screen.dart';

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

  // Tarifs paramétrables
  List<Map<String, dynamic>> _tarifs = [];
  String? _tarifId;             // id du tarif sélectionné (null = mode legacy)
  bool _utiliserTarif = true;   // toggle UI

  // Limites par colis (refus auto)
  final _longueurMaxCtrl = TextEditingController();
  final _largeurMaxCtrl = TextEditingController();
  final _hauteurMaxCtrl = TextEditingController();
  final _poidsMaxColisCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _loadTarifs();
  }

  Future<void> _loadTarifs() async {
    final api = context.read<ApiService>();
    final res = await api.getMesTarifs();
    if (!mounted) return;
    final actifs = res.where((t) => (t as Map<String, dynamic>)['estActif'] == true).cast<Map<String, dynamic>>().toList();
    setState(() {
      _tarifs = actifs;
      _ajusterTarifSelectionne();
    });
  }

  /// Tarifs applicables à la date du trajet (filtre période haute/basse saison).
  /// Un tarif est applicable si _dateDepart est dans [DateDebut, DateFin] (bornes nullable).
  List<Map<String, dynamic>> get _tarifsApplicables {
    final d = DateUtils.dateOnly(_dateDepart);
    return _tarifs.where((t) {
      final dd = DateTime.tryParse(t['dateDebut']?.toString() ?? '');
      final df = DateTime.tryParse(t['dateFin']?.toString() ?? '');
      if (dd != null && d.isBefore(DateUtils.dateOnly(dd))) return false;
      if (df != null && d.isAfter(DateUtils.dateOnly(df))) return false;
      return true;
    }).toList();
  }

  /// Si le tarif sélectionné n'est plus applicable (changement de date), on retombe sur le premier dispo.
  void _ajusterTarifSelectionne() {
    final dispos = _tarifsApplicables;
    if (dispos.isEmpty) {
      _tarifId = null;
      _utiliserTarif = false;
      return;
    }
    final stillValid = _tarifId != null && dispos.any((t) => t['id'] == _tarifId);
    if (!stillValid) _tarifId = dispos.first['id'];
  }

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
        _ajusterTarifSelectionne();
      });
    }
  }

  Future<Map<String, dynamic>?> _selectRelais({String? label}) async {
    final api = context.read<ApiService>();
    final relais = await api.getRelaisDisponibles();
    final perso = await api.getMesPoints();
    if (!mounted) return null;
    // Tag chaque item avec son type pour pouvoir router à la création
    final relaisTagged = relais.map((r) => {...r as Map<String, dynamic>, 'type': 'officiel'}).toList();
    final persoTagged = perso.map((p) => {
          ...p as Map<String, dynamic>,
          'type': 'perso',
          'nomRelais': p['nom'], // pour réutiliser le rendu existant
        }).toList();
    if (relaisTagged.isEmpty && persoTagged.isEmpty) {
      setState(() => _error = 'Aucun point disponible (créez d\'abord un point perso ou demandez des relais).');
      return null;
    }
    return showModalBottomSheet<Map<String, dynamic>>(
      context: context, isScrollControlled: true,
      builder: (ctx) => _RelaisPickerSheet(
        relaisList: relaisTagged,
        persoList: persoTagged,
        label: label ?? 'Sélectionner un point',
      ),
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
      // date + heure locales → UTC : garde le bon jour à l'affichage (sauf heure < 2h du matin)
      'dateDepart': DateTime(_dateDepart.year, _dateDepart.month, _dateDepart.day, _heureDepart.hour, _heureDepart.minute).toUtc().toIso8601String(),
      'dateEstimeeArrivee': DateTime(_dateArrivee.year, _dateArrivee.month, _dateArrivee.day, _heureArrivee.hour, _heureArrivee.minute).toUtc().toIso8601String(),
      'capaciteMaxPoids': double.tryParse(_poidsMaxCtrl.text) ?? 500,
      'nombreMaxColis': int.tryParse(_nbColisCtrl.text) ?? 30,
      'modeTarification': _modeTarif == 'PrixParColis' ? 0 : (_modeTarif == 'PrixAuKilo' ? 1 : 2),
      if (!_utiliserTarif && (_modeTarif == 'PrixParColis' || _modeTarif == 'Forfait')) 'prixParColis': double.tryParse(_prixCtrl.text) ?? 0,
      if (!_utiliserTarif && (_modeTarif == 'PrixAuKilo' || _modeTarif == 'Forfait')) 'prixAuKilo': double.tryParse(_prixCtrl.text) ?? 0,
      if (_utiliserTarif && _tarifId != null) 'tarifId': _tarifId,
      // RelaisDepartId : seulement si on a choisi un relais OFFICIEL (sinon laissé null pour départ depuis point perso)
      if (_relaisDepart != null && _relaisDepart!['type'] != 'perso') 'relaisDepartId': _relaisDepart!['id'],
      if (_longueurMaxCtrl.text.isNotEmpty) 'longueurMaxColisCm': int.tryParse(_longueurMaxCtrl.text),
      if (_largeurMaxCtrl.text.isNotEmpty) 'largeurMaxColisCm': int.tryParse(_largeurMaxCtrl.text),
      if (_hauteurMaxCtrl.text.isNotEmpty) 'hauteurMaxColisCm': int.tryParse(_hauteurMaxCtrl.text),
      if (_poidsMaxColisCtrl.text.isNotEmpty) 'poidsMaxColisKg': double.tryParse(_poidsMaxColisCtrl.text),
      'conditions': _conditionsCtrl.text.trim(),
    };

    final res = await api.createTrajet(data);
    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; }); return;
    }

    final trajetId = res['id'];

    // Ajouter étape départ
    final depDt = DateTime(_dateDepart.year, _dateDepart.month, _dateDepart.day, _heureDepart.hour, _heureDepart.minute);
    final depPerso = _relaisDepart!['type'] == 'perso';
    await api.addEtape(trajetId, _relaisDepart!['id'], depDt.toUtc().toIso8601String(), perso: depPerso);

    // Ajouter étapes intermédiaires
    for (final e in _etapes) {
      final h = e['heure'] as TimeOfDay;
      final d = e['date'] as DateTime? ?? _dateDepart;
      final dt = DateTime(d.year, d.month, d.day, h.hour, h.minute);
      final r = e['relais'] as Map<String, dynamic>;
      final perso = r['type'] == 'perso';
      await api.addEtape(trajetId, r['id'], dt.toUtc().toIso8601String(), perso: perso);
    }

    // Ajouter étape arrivée
    final arrDt = DateTime(_dateArrivee.year, _dateArrivee.month, _dateArrivee.day, _heureArrivee.hour, _heureArrivee.minute);
    final arrPerso = _relaisArrivee!['type'] == 'perso';
    await api.addEtape(trajetId, _relaisArrivee!['id'], arrDt.toUtc().toIso8601String(), perso: arrPerso);

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
            if (_relaisDepart != null) ...[
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
              _ouvertureBadge(_relaisDepart!, _dateDepart, _heureDepart),
            ],

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
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
                      child: _ouvertureBadge(r, d, h),
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
            if (_relaisArrivee != null) ...[
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
              _ouvertureBadge(_relaisArrivee!, _dateArrivee, _heureArrivee),
            ],

            const SizedBox(height: 20), _label('CAPACITÉ'),
            Row(children: [
              Expanded(child: TextField(controller: _poidsMaxCtrl, decoration: const InputDecoration(labelText: 'POIDS MAX (KG)'), keyboardType: TextInputType.number)),
              const SizedBox(width: 10),
              Expanded(child: TextField(controller: _nbColisCtrl, decoration: const InputDecoration(labelText: 'NB COLIS MAX'), keyboardType: TextInputType.number)),
            ]),

            const SizedBox(height: 20), _label('TARIFICATION'),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              value: _utiliserTarif,
              onChanged: _tarifsApplicables.isEmpty ? null : (v) => setState(() => _utiliserTarif = v),
              title: const Text('Utiliser un tarif enregistré', style: TextStyle(fontWeight: FontWeight.w700)),
              subtitle: Text(_tarifs.isEmpty
                  ? 'Aucun tarif enregistré — créez-en un d\'abord'
                  : (_tarifsApplicables.isEmpty
                      ? 'Aucun tarif valide pour la date du trajet (tous expirés ou pas encore actifs)'
                      : 'Sélection d\'un de vos templates de tarifs (filtrés selon la date du trajet)')),
            ),
            if (_utiliserTarif && _tarifsApplicables.isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    DropdownButtonFormField<String>(
                      initialValue: _tarifId,
                      decoration: const InputDecoration(labelText: 'TARIF'),
                      items: _tarifsApplicables.map((t) => DropdownMenuItem(
                        value: t['id'] as String,
                        child: Text(t['nom']?.toString() ?? '—'),
                      )).toList(),
                      onChanged: (v) => setState(() => _tarifId = v),
                    ),
                    if (_tarifId != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 6),
                        child: _tarifPreview(),
                      ),
                  ],
                ),
              )
            else ...[
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
            ],
            const SizedBox(height: 8),
            TextButton.icon(
              icon: const Icon(Icons.tune, size: 18),
              label: const Text('Configurer un nouveau tarif'),
              onPressed: () async {
                await Navigator.push(context, MaterialPageRoute(builder: (_) => const TarifsScreen()));
                _loadTarifs();
              },
            ),

            const SizedBox(height: 20), _label('LIMITES PAR COLIS (REFUS AUTO SI DÉPASSÉ)'),
            const Text('Optionnel. Laisser vide = aucune limite individuelle.', style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
            const SizedBox(height: 8),
            Row(children: [
              Expanded(child: TextField(controller: _longueurMaxCtrl, decoration: const InputDecoration(labelText: 'L max (cm)'), keyboardType: TextInputType.number)),
              const SizedBox(width: 6),
              Expanded(child: TextField(controller: _largeurMaxCtrl, decoration: const InputDecoration(labelText: 'l max (cm)'), keyboardType: TextInputType.number)),
              const SizedBox(width: 6),
              Expanded(child: TextField(controller: _hauteurMaxCtrl, decoration: const InputDecoration(labelText: 'H max (cm)'), keyboardType: TextInputType.number)),
              const SizedBox(width: 6),
              Expanded(child: TextField(controller: _poidsMaxColisCtrl, decoration: const InputDecoration(labelText: 'Poids/colis (kg)'), keyboardType: TextInputType.number)),
            ]),

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

  Widget _tarifPreview() {
    final t = _tarifs.firstWhere((x) => x['id'] == _tarifId, orElse: () => {});
    if (t.isEmpty) return const SizedBox.shrink();
    return Container(
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: AppTheme.primary.withValues(alpha: 0.06),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Standard : ${t['prixAuKiloStandard']} €/kg jusqu\'à ${t['seuilStandardKg']} kg',
              style: const TextStyle(fontSize: 12)),
          Text('Lourd : ${t['forfaitLourd']} € + ${t['prixAuKiloLourd']} €/kg au-delà',
              style: const TextStyle(fontSize: 12)),
          Text('Hors gabarit (> ${t['longueurMaxStandardCm']}×${t['largeurMaxStandardCm']}×${t['hauteurMaxStandardCm']} cm) : ${t['forfaitHorsGabarit']} € + ${t['prixAuKiloHorsGabarit']} €/kg',
              style: const TextStyle(fontSize: 12)),
        ],
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

  /// Badge visuel Ouvert / Fermé selon les horaires du relais à la date+heure choisies.
  /// - Points perso : toujours "Point perso" en info (le transporteur gère lui-même)
  /// - Relais officiel : compare joursOuverture + heureOuverture/Fermeture
  Widget _ouvertureBadge(Map<String, dynamic> relais, DateTime date, TimeOfDay heure) {
    if (relais['type'] == 'perso') {
      return Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
        decoration: BoxDecoration(
          color: AppTheme.accent.withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(6),
        ),
        child: const Row(mainAxisSize: MainAxisSize.min, children: [
          Icon(Icons.place, size: 14, color: AppTheme.accent),
          SizedBox(width: 6),
          Flexible(child: Text('Point perso : gestion libre (pas d\'horaires)',
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppTheme.accent))),
        ]),
      );
    }

    final hoStr = relais['heureOuverture']?.toString();
    final hfStr = relais['heureFermeture']?.toString();
    final joursStr = (relais['joursOuverture'] ?? '').toString();
    if (hoStr == null || hfStr == null || joursStr.isEmpty) {
      return Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
        decoration: BoxDecoration(
          color: AppTheme.textMuted.withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(6),
        ),
        child: const Text('Horaires du relais non renseignés',
            style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
      );
    }

    // Match jour
    const joursMap = {1: 'Lun', 2: 'Mar', 3: 'Mer', 4: 'Jeu', 5: 'Ven', 6: 'Sam', 7: 'Dim'};
    final jourStr = joursMap[date.weekday] ?? '';
    final joursNormalises = joursStr.split(RegExp(r'[,;\s]+')).map((s) => s.trim()).toList();
    final jourOuvert = joursNormalises.contains(jourStr);

    // Match heure : parse "HH:mm" et compare aux minutes du jour
    int? toMinutes(String hhmm) {
      final parts = hhmm.split(':');
      if (parts.length != 2) return null;
      final h = int.tryParse(parts[0]), m = int.tryParse(parts[1]);
      if (h == null || m == null) return null;
      return h * 60 + m;
    }
    final ho = toMinutes(hoStr), hf = toMinutes(hfStr);
    final actuel = heure.hour * 60 + heure.minute;
    final heureOk = ho != null && hf != null && actuel >= ho && actuel <= hf;

    final ouvert = jourOuvert && heureOk;
    final color = ouvert ? AppTheme.success : AppTheme.danger;
    final icon = ouvert ? Icons.check_circle : Icons.cancel;
    final label = ouvert
        ? 'Ouvert (horaires $hoStr–$hfStr, $joursStr)'
        : !jourOuvert
            ? 'Fermé ce jour ($jourStr) — horaires : $joursStr'
            : 'Fermé à cette heure — horaires $hoStr–$hfStr';
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        Icon(icon, size: 14, color: color),
        const SizedBox(width: 6),
        Flexible(child: Text(label,
            style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: color))),
      ]),
    );
  }
}

class _RelaisPickerSheet extends StatefulWidget {
  final List<dynamic> relaisList;
  final List<dynamic> persoList;
  final String label;
  const _RelaisPickerSheet({required this.relaisList, required this.persoList, required this.label});

  @override
  State<_RelaisPickerSheet> createState() => _RelaisPickerSheetState();
}

class _RelaisPickerSheetState extends State<_RelaisPickerSheet> with SingleTickerProviderStateMixin {
  String _search = '';
  late TabController _tabCtrl;

  @override
  void initState() {
    super.initState();
    // Si pas de relais officiels, on ouvre direct sur l'onglet perso
    final initialIndex = widget.relaisList.isEmpty && widget.persoList.isNotEmpty ? 1 : 0;
    _tabCtrl = TabController(length: 2, vsync: this, initialIndex: initialIndex);
  }

  @override
  void dispose() { _tabCtrl.dispose(); super.dispose(); }

  List<dynamic> _filter(List<dynamic> source) {
    if (_search.isEmpty) return source;
    final q = _search.toLowerCase();
    return source.where((r) {
      final m = r as Map<String, dynamic>;
      final nom = (m['nomRelais'] ?? m['nom'] ?? '').toString().toLowerCase();
      final ville = (m['ville'] ?? '').toString().toLowerCase();
      final dept = (m['departement'] ?? '').toString().toLowerCase();
      final region = (m['region'] ?? '').toString().toLowerCase();
      final pays = (m['pays'] ?? '').toString().toLowerCase();
      return nom.contains(q) || ville.contains(q) || dept.contains(q) || region.contains(q) || pays.contains(q);
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final relaisFiltered = _filter(widget.relaisList);
    final persoFiltered = _filter(widget.persoList);
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
            TabBar(
              controller: _tabCtrl,
              labelColor: AppTheme.primary,
              unselectedLabelColor: AppTheme.textMuted,
              tabs: [
                Tab(text: 'Relais officiels (${widget.relaisList.length})'),
                Tab(text: 'Mes points perso (${widget.persoList.length})'),
              ],
            ),
            const SizedBox(height: 8),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: TextField(
                decoration: const InputDecoration(labelText: 'RECHERCHER', hintText: 'Nom, ville, département...', prefixIcon: Icon(Icons.search, size: 20)),
                onChanged: (v) => setState(() => _search = v),
              ),
            ),
            const SizedBox(height: 8),
            Expanded(
              child: TabBarView(
                controller: _tabCtrl,
                children: [
                  _buildRelaisList(relaisFiltered, scrollCtrl),
                  _buildPersoList(persoFiltered, scrollCtrl),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRelaisList(List<dynamic> items, ScrollController scrollCtrl) {
    if (items.isEmpty) {
      return const Center(child: Padding(padding: EdgeInsets.all(24), child: Text('Aucun relais officiel correspondant.', style: TextStyle(color: AppTheme.textMuted))));
    }
    return ListView.builder(
      controller: scrollCtrl, padding: const EdgeInsets.symmetric(horizontal: 16),
      itemCount: items.length,
      itemBuilder: (ctx, i) {
        final r = items[i] as Map<String, dynamic>;
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
    );
  }

  Widget _buildPersoList(List<dynamic> items, ScrollController scrollCtrl) {
    if (items.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'Aucun point perso.\n\nCréez-en depuis Profil → Mes points perso pour utiliser vos lieux personnels (garage, atelier…) comme étape de trajet.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppTheme.textMuted),
          ),
        ),
      );
    }
    return ListView.builder(
      controller: scrollCtrl, padding: const EdgeInsets.symmetric(horizontal: 16),
      itemCount: items.length,
      itemBuilder: (ctx, i) {
        final p = items[i] as Map<String, dynamic>;
        return Card(
          child: ListTile(
            onTap: () => Navigator.pop(context, p),
            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
            leading: const Icon(Icons.place, color: AppTheme.accent),
            title: Text(p['nom'] ?? p['nomRelais'] ?? '—', style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
            subtitle: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('${p['adresse'] ?? ''}', style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                Text('${p['codePostal'] ?? ''} ${p['ville']} — ${p['pays']}', style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                if ((p['horaires'] ?? '').toString().isNotEmpty)
                  Text('🕒 ${p['horaires']}', style: const TextStyle(fontSize: 11, color: AppTheme.textMuted)),
              ],
            ),
          ),
        );
      },
    );
  }
}
