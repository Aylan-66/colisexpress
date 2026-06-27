import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class TarifEditScreen extends StatefulWidget {
  final Map<String, dynamic>? tarif;
  const TarifEditScreen({super.key, this.tarif});

  @override
  State<TarifEditScreen> createState() => _TarifEditScreenState();
}

class _TarifEditScreenState extends State<TarifEditScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nom = TextEditingController();
  final _description = TextEditingController();

  final _prixStd = TextEditingController(text: '5');
  final _seuilStd = TextEditingController(text: '10');
  final _forfaitLourd = TextEditingController(text: '20');
  final _prixLourd = TextEditingController(text: '4');
  final _forfaitHG = TextEditingController(text: '50');
  final _prixHG = TextEditingController(text: '6');
  final _longueurMax = TextEditingController(text: '60');
  final _largeurMax = TextEditingController(text: '40');
  final _hauteurMax = TextEditingController(text: '40');

  // Période de validité (haute/basse saison)
  DateTime? _dateDebut;
  DateTime? _dateFin;

  // Simulation
  final _simPoids = TextEditingController(text: '5');
  final _simL = TextEditingController();
  final _simLa = TextEditingController();
  final _simH = TextEditingController();
  double? _simPrix;

  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final t = widget.tarif;
    if (t != null) {
      _nom.text = t['nom']?.toString() ?? '';
      _description.text = t['description']?.toString() ?? '';
      _prixStd.text = t['prixAuKiloStandard']?.toString() ?? '';
      _seuilStd.text = t['seuilStandardKg']?.toString() ?? '';
      _forfaitLourd.text = t['forfaitLourd']?.toString() ?? '';
      _prixLourd.text = t['prixAuKiloLourd']?.toString() ?? '';
      _forfaitHG.text = t['forfaitHorsGabarit']?.toString() ?? '';
      _prixHG.text = t['prixAuKiloHorsGabarit']?.toString() ?? '';
      _longueurMax.text = t['longueurMaxStandardCm']?.toString() ?? '';
      _largeurMax.text = t['largeurMaxStandardCm']?.toString() ?? '';
      _hauteurMax.text = t['hauteurMaxStandardCm']?.toString() ?? '';
      _dateDebut = DateTime.tryParse(t['dateDebut']?.toString() ?? '');
      _dateFin = DateTime.tryParse(t['dateFin']?.toString() ?? '');
    }
  }

  Map<String, dynamic> _buildPayload() => {
        'nom': _nom.text.trim(),
        'description': _description.text.trim(),
        if (_dateDebut != null) 'dateDebut': _dateDebut!.toIso8601String(),
        if (_dateFin != null) 'dateFin': _dateFin!.toIso8601String(),
        'prixAuKiloStandard': double.tryParse(_prixStd.text) ?? 0,
        'seuilStandardKg': double.tryParse(_seuilStd.text) ?? 0,
        'forfaitLourd': double.tryParse(_forfaitLourd.text) ?? 0,
        'prixAuKiloLourd': double.tryParse(_prixLourd.text) ?? 0,
        'forfaitHorsGabarit': double.tryParse(_forfaitHG.text) ?? 0,
        'prixAuKiloHorsGabarit': double.tryParse(_prixHG.text) ?? 0,
        'longueurMaxStandardCm': int.tryParse(_longueurMax.text) ?? 0,
        'largeurMaxStandardCm': int.tryParse(_largeurMax.text) ?? 0,
        'hauteurMaxStandardCm': int.tryParse(_hauteurMax.text) ?? 0,
      };

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    final api = context.read<ApiService>();
    final payload = _buildPayload();
    final res = widget.tarif == null
        ? await api.createTarif(payload)
        : await api.updateTarif(widget.tarif!['id'], payload);
    if (!mounted) return;
    setState(() => _saving = false);
    if (res.containsKey('error')) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['error'])));
    } else {
      Navigator.pop(context);
    }
  }

  Future<void> _simuler() async {
    final api = context.read<ApiService>();
    final payload = _buildPayload();
    payload['poidsKg'] = double.tryParse(_simPoids.text) ?? 0;
    if (_simL.text.isNotEmpty) payload['longueurCm'] = int.tryParse(_simL.text);
    if (_simLa.text.isNotEmpty) payload['largeurCm'] = int.tryParse(_simLa.text);
    if (_simH.text.isNotEmpty) payload['hauteurCm'] = int.tryParse(_simH.text);
    final res = await api.simulerTarif(payload);
    if (!mounted) return;
    setState(() => _simPrix = (res['prix'] as num?)?.toDouble());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.tarif == null ? 'Nouveau tarif' : 'Modifier le tarif')),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            TextFormField(
              controller: _nom,
              decoration: const InputDecoration(labelText: 'Nom du tarif *', hintText: 'Ex : Tarif Paris-Maghreb'),
              validator: (v) => (v == null || v.trim().isEmpty) ? 'Obligatoire' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _description,
              decoration: const InputDecoration(labelText: 'Description (optionnel)'),
              maxLines: 2,
            ),
            const SizedBox(height: 20),

            _section('Période de validité (haute/basse saison)'),
            const Text('Optionnel — laissez vide pour un tarif permanent. Sert à organiser plusieurs tarifs par saison.',
                style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
            const SizedBox(height: 8),
            Row(children: [
              Expanded(child: _datePickerField('Du', _dateDebut, (d) => setState(() => _dateDebut = d))),
              const SizedBox(width: 8),
              Expanded(child: _datePickerField('Au', _dateFin, (d) => setState(() => _dateFin = d))),
            ]),
            const SizedBox(height: 24),

            _section('Palier standard'),
            const Text('Pour les colis légers, prix au kilo simple jusqu\'au seuil.',
                style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
            const SizedBox(height: 8),
            Row(children: [
              Expanded(child: _numField(_prixStd, 'Prix au kilo (€)')),
              const SizedBox(width: 8),
              Expanded(child: _numField(_seuilStd, 'Seuil (kg)')),
            ]),
            const SizedBox(height: 24),

            _section('Palier lourd'),
            const Text('Au-delà du seuil standard, forfait + prix au kilo.',
                style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
            const SizedBox(height: 8),
            Row(children: [
              Expanded(child: _numField(_forfaitLourd, 'Forfait (€)')),
              const SizedBox(width: 8),
              Expanded(child: _numField(_prixLourd, 'Prix au kilo (€)')),
            ]),
            const SizedBox(height: 24),

            _section('Hors gabarit'),
            const Text('Limites de taille standard (au-delà = hors gabarit).',
                style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
            const SizedBox(height: 8),
            Row(children: [
              Expanded(child: _numField(_longueurMax, 'Longueur max (cm)', isInt: true)),
              const SizedBox(width: 8),
              Expanded(child: _numField(_largeurMax, 'Largeur max (cm)', isInt: true)),
              const SizedBox(width: 8),
              Expanded(child: _numField(_hauteurMax, 'Hauteur max (cm)', isInt: true)),
            ]),
            const SizedBox(height: 8),
            Row(children: [
              Expanded(child: _numField(_forfaitHG, 'Forfait (€)')),
              const SizedBox(width: 8),
              Expanded(child: _numField(_prixHG, 'Prix au kilo (€)')),
            ]),
            const SizedBox(height: 24),

            _section('Prévisualiser'),
            Row(children: [
              Expanded(child: _numField(_simPoids, 'Poids (kg)')),
              const SizedBox(width: 8),
              Expanded(child: _numField(_simL, 'L (cm)', isInt: true, optional: true)),
              const SizedBox(width: 8),
              Expanded(child: _numField(_simLa, 'l (cm)', isInt: true, optional: true)),
              const SizedBox(width: 8),
              Expanded(child: _numField(_simH, 'H (cm)', isInt: true, optional: true)),
            ]),
            const SizedBox(height: 8),
            OutlinedButton.icon(
              onPressed: _simuler,
              icon: const Icon(Icons.calculate, size: 18),
              label: const Text('Calculer le prix'),
            ),
            if (_simPrix != null)
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: AppTheme.success.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text('Prix calculé : ${_simPrix!.toStringAsFixed(2)} €',
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: AppTheme.success)),
                ),
              ),

            const SizedBox(height: 32),
            ElevatedButton(
              onPressed: _saving ? null : _save,
              child: _saving
                  ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : Text(widget.tarif == null ? 'Créer le tarif' : 'Enregistrer'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _datePickerField(String label, DateTime? value, ValueChanged<DateTime?> onChanged) {
    final txt = value != null
        ? '${value.day.toString().padLeft(2, '0')}/${value.month.toString().padLeft(2, '0')}/${value.year}'
        : '—';
    return InkWell(
      onTap: () async {
        final picked = await showDatePicker(
          context: context,
          initialDate: value ?? DateTime.now(),
          firstDate: DateTime.now().subtract(const Duration(days: 365)),
          lastDate: DateTime.now().add(const Duration(days: 365 * 3)),
        );
        if (picked != null) onChanged(picked);
      },
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          suffixIcon: value != null
              ? IconButton(icon: const Icon(Icons.close, size: 16), onPressed: () => onChanged(null))
              : const Icon(Icons.calendar_today, size: 18),
        ),
        child: Text(txt, style: const TextStyle(fontSize: 14)),
      ),
    );
  }

  Widget _section(String t) => Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: Text(t, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w800)),
      );

  Widget _numField(TextEditingController c, String label, {bool isInt = false, bool optional = false}) =>
      TextFormField(
        controller: c,
        keyboardType: TextInputType.numberWithOptions(decimal: !isInt),
        decoration: InputDecoration(labelText: label),
        validator: optional
            ? null
            : (v) {
                if (v == null || v.trim().isEmpty) return 'Requis';
                final n = isInt ? int.tryParse(v) : double.tryParse(v);
                if (n == null) return 'Nombre invalide';
                if ((isInt && (n as int) <= 0) || (!isInt && (n as double) < 0)) return 'Valeur invalide';
                return null;
              },
      );
}
