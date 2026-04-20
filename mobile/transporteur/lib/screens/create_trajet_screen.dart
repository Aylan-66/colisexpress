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
  final _pointDepotCtrl = TextEditingController();
  final _conditionsCtrl = TextEditingController();

  String _paysDepart = 'France';
  String _paysArrivee = 'Algérie';
  String _modeTarif = 'PrixParColis';
  DateTime _dateDepart = DateTime.now().add(const Duration(days: 3));
  DateTime _dateArrivee = DateTime.now().add(const Duration(days: 6));
  bool _loading = false;
  String? _error;

  Future<void> _pickDate(bool isDepart) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: isDepart ? _dateDepart : _dateArrivee,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      setState(() {
        if (isDepart) {
          _dateDepart = picked;
          if (_dateArrivee.isBefore(_dateDepart)) {
            _dateArrivee = _dateDepart.add(const Duration(days: 3));
          }
        } else {
          _dateArrivee = picked;
        }
      });
    }
  }

  Future<void> _submit() async {
    if (_villeDepartCtrl.text.isEmpty || _villeArriveeCtrl.text.isEmpty) {
      setState(() => _error = 'Remplissez les villes de départ et d\'arrivée.');
      return;
    }
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
      if (_modeTarif == 'PrixParColis' || _modeTarif == 'Forfait')
        'prixParColis': double.tryParse(_prixCtrl.text) ?? 0,
      if (_modeTarif == 'PrixAuKilo' || _modeTarif == 'Forfait')
        'prixAuKilo': double.tryParse(_prixCtrl.text) ?? 0,
      'pointDepot': _pointDepotCtrl.text.trim(),
      'conditions': _conditionsCtrl.text.trim(),
    };

    final res = await api.createTrajet(data);
    setState(() => _loading = false);

    if (res.containsKey('error')) {
      setState(() => _error = res['error']);
    } else if (mounted) {
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
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                  color: AppTheme.danger.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(_error!, style: const TextStyle(color: AppTheme.danger, fontSize: 13)),
              ),

            const Text('TRAJET', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
            const SizedBox(height: 10),

            DropdownButtonFormField<String>(
              value: _paysDepart,
              decoration: const InputDecoration(labelText: 'PAYS DÉPART'),
              items: ['France', 'Espagne'].map((p) => DropdownMenuItem(value: p, child: Text(p))).toList(),
              onChanged: (v) => setState(() => _paysDepart = v!),
            ),
            const SizedBox(height: 10),
            TextField(controller: _villeDepartCtrl, decoration: const InputDecoration(labelText: 'VILLE DÉPART', hintText: 'Paris')),
            const SizedBox(height: 10),

            DropdownButtonFormField<String>(
              value: _paysArrivee,
              decoration: const InputDecoration(labelText: 'PAYS ARRIVÉE'),
              items: ['Algérie', 'Maroc', 'Tunisie'].map((p) => DropdownMenuItem(value: p, child: Text(p))).toList(),
              onChanged: (v) => setState(() => _paysArrivee = v!),
            ),
            const SizedBox(height: 10),
            TextField(controller: _villeArriveeCtrl, decoration: const InputDecoration(labelText: 'VILLE ARRIVÉE', hintText: 'Alger')),

            const SizedBox(height: 20),
            const Text('DATES', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
            const SizedBox(height: 10),

            Row(children: [
              Expanded(
                child: OutlinedButton.icon(
                  icon: const Icon(Icons.calendar_today, size: 16),
                  label: Text('Départ: ${_fmt(_dateDepart)}'),
                  onPressed: () => _pickDate(true),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: OutlinedButton.icon(
                  icon: const Icon(Icons.calendar_today, size: 16),
                  label: Text('Arrivée: ${_fmt(_dateArrivee)}'),
                  onPressed: () => _pickDate(false),
                ),
              ),
            ]),

            const SizedBox(height: 20),
            const Text('CAPACITÉ', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
            const SizedBox(height: 10),

            Row(children: [
              Expanded(child: TextField(controller: _poidsMaxCtrl, decoration: const InputDecoration(labelText: 'POIDS MAX (KG)'), keyboardType: TextInputType.number)),
              const SizedBox(width: 10),
              Expanded(child: TextField(controller: _nbColisCtrl, decoration: const InputDecoration(labelText: 'NB COLIS MAX'), keyboardType: TextInputType.number)),
            ]),

            const SizedBox(height: 20),
            const Text('TARIFICATION', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
            const SizedBox(height: 10),

            DropdownButtonFormField<String>(
              value: _modeTarif,
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
              decoration: InputDecoration(
                labelText: _modeTarif == 'PrixAuKilo' ? 'PRIX AU KILO (€)' : 'PRIX PAR COLIS (€)',
                hintText: _modeTarif == 'PrixAuKilo' ? '7' : '85',
              ),
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
            ),

            const SizedBox(height: 20),
            const Text('INFOS PRATIQUES', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
            const SizedBox(height: 10),

            TextField(controller: _pointDepotCtrl, decoration: const InputDecoration(labelText: 'POINT DE DÉPÔT', hintText: 'Paris 15e — 42 rue...')),
            const SizedBox(height: 10),
            TextField(controller: _conditionsCtrl, decoration: const InputDecoration(labelText: 'CONDITIONS (OPTIONNEL)'), maxLines: 3),

            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _loading ? null : _submit,
                style: ElevatedButton.styleFrom(backgroundColor: AppTheme.accent),
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
}
