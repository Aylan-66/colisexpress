import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import '../main.dart';

class ProfilScreen extends StatefulWidget {
  const ProfilScreen({super.key});

  @override
  State<ProfilScreen> createState() => _ProfilScreenState();
}

class _ProfilScreenState extends State<ProfilScreen> {
  Map<String, dynamic>? _profil;
  bool _loading = true;
  bool _saving = false;
  String? _error;
  String? _success;

  final _adresseCtrl = TextEditingController();
  final _commissionMontantCtrl = TextEditingController();
  TimeOfDay _heureOuverture = const TimeOfDay(hour: 9, minute: 0);
  TimeOfDay _heureFermeture = const TimeOfDay(hour: 18, minute: 0);
  TimeOfDay _heureOuvertureWe = const TimeOfDay(hour: 10, minute: 0);
  TimeOfDay _heureFermetureWe = const TimeOfDay(hour: 14, minute: 0);
  String _commissionType = 'Fixe';
  final Set<String> _joursOuverture = {'Lun', 'Mar', 'Mer', 'Jeu', 'Ven'};

  static const _tousLesJours = ['Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam', 'Dim'];

  @override
  void initState() {
    super.initState();
    _load();
  }

  TimeOfDay _parseTime(String? s, TimeOfDay fallback) {
    if (s == null || !s.contains(':')) return fallback;
    final parts = s.split(':');
    return TimeOfDay(hour: int.tryParse(parts[0]) ?? fallback.hour, minute: int.tryParse(parts[1]) ?? fallback.minute);
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    final res = await context.read<ApiService>().getProfil();
    if (!mounted) return;

    if (res.containsKey('error')) {
      setState(() { _loading = false; _error = res['error']; });
      return;
    }

    setState(() {
      _profil = res;
      _adresseCtrl.text = res['adresse']?.toString() ?? '';
      _commissionMontantCtrl.text = (res['montantCommission'] ?? 0).toString();
      _commissionType = res['typeCommission']?.toString() ?? 'Fixe';
      _heureOuverture = _parseTime(res['heureOuverture']?.toString(), const TimeOfDay(hour: 9, minute: 0));
      _heureFermeture = _parseTime(res['heureFermeture']?.toString(), const TimeOfDay(hour: 18, minute: 0));
      _heureOuvertureWe = _parseTime(res['heureOuvertureWeekend']?.toString(), const TimeOfDay(hour: 10, minute: 0));
      _heureFermetureWe = _parseTime(res['heureFermetureWeekend']?.toString(), const TimeOfDay(hour: 14, minute: 0));

      final jours = res['joursOuverture']?.toString();
      if (jours != null && jours.isNotEmpty) {
        _joursOuverture..clear()..addAll(jours.split(',').map((j) => j.trim()));
      }
      _loading = false;
    });
  }

  String _fmt(TimeOfDay t) => '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  Future<void> _pickTime(String label, TimeOfDay current, void Function(TimeOfDay) onPicked) async {
    final picked = await showTimePicker(context: context, initialTime: current, helpText: label);
    if (picked != null) setState(() => onPicked(picked));
  }

  bool get _hasWeekend => _joursOuverture.contains('Sam') || _joursOuverture.contains('Dim');

  Future<void> _save() async {
    setState(() { _saving = true; _error = null; _success = null; });
    final res = await context.read<ApiService>().updateProfil({
      'adresse': _adresseCtrl.text.trim(),
      'heureOuverture': _fmt(_heureOuverture),
      'heureFermeture': _fmt(_heureFermeture),
      if (_hasWeekend) 'heureOuvertureWeekend': _fmt(_heureOuvertureWe),
      if (_hasWeekend) 'heureFermetureWeekend': _fmt(_heureFermetureWe),
      'joursOuverture': _joursOuverture.join(','),
      'typeCommission': _commissionType,
      'montantCommission': double.tryParse(_commissionMontantCtrl.text.trim()) ?? 0,
    });
    if (!mounted) return;
    setState(() {
      _saving = false;
      if (res.containsKey('error')) _error = res['error'];
      else _success = 'Profil mis à jour.';
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Mon profil')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  if (_success != null) _alert(_success!, AppTheme.success),
                  if (_error != null) _alert(_error!, AppTheme.danger),

                  Center(
                    child: Container(
                      width: 80, height: 80,
                      decoration: BoxDecoration(color: AppTheme.primary, borderRadius: BorderRadius.circular(40)),
                      child: const Icon(Icons.store, size: 40, color: Colors.white),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Center(child: Text(_profil?['nomRelais']?.toString() ?? 'Point Relais',
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800))),
                  Center(child: Text('${_profil?['ville'] ?? ''}, ${_profil?['pays'] ?? ''}',
                      style: const TextStyle(color: AppTheme.textMuted, fontSize: 13))),
                  const SizedBox(height: 20),

                  // Adresse
                  _section('Adresse', [
                    TextField(controller: _adresseCtrl, decoration: const InputDecoration(labelText: 'ADRESSE'), maxLines: 2),
                  ]),

                  // Jours d'ouverture
                  _section('Jours d\'ouverture', [
                    Wrap(
                      spacing: 6, runSpacing: 6,
                      children: _tousLesJours.map((j) => FilterChip(
                        label: Text(j, style: const TextStyle(fontSize: 12)),
                        selected: _joursOuverture.contains(j),
                        onSelected: (s) => setState(() => s ? _joursOuverture.add(j) : _joursOuverture.remove(j)),
                        selectedColor: AppTheme.primary.withValues(alpha: 0.15),
                        checkmarkColor: AppTheme.primary,
                      )).toList(),
                    ),
                  ]),

                  // Horaires semaine
                  _section('Horaires semaine (Lun-Ven)', [
                    Row(children: [
                      Expanded(child: _timeButton('Ouverture', _heureOuverture, (t) => _heureOuverture = t)),
                      const SizedBox(width: 12),
                      Expanded(child: _timeButton('Fermeture', _heureFermeture, (t) => _heureFermeture = t)),
                    ]),
                  ]),

                  // Horaires weekend
                  if (_hasWeekend)
                    _section('Horaires weekend (Sam-Dim)', [
                      Row(children: [
                        Expanded(child: _timeButton('Ouverture', _heureOuvertureWe, (t) => _heureOuvertureWe = t)),
                        const SizedBox(width: 12),
                        Expanded(child: _timeButton('Fermeture', _heureFermetureWe, (t) => _heureFermetureWe = t)),
                      ]),
                    ]),

                  // Commission
                  _section('Commission', [
                    DropdownButtonFormField<String>(
                      initialValue: _commissionType,
                      decoration: const InputDecoration(labelText: 'TYPE'),
                      items: ['Fixe', 'Pourcentage'].map((t) => DropdownMenuItem(value: t, child: Text(t))).toList(),
                      onChanged: (v) => setState(() => _commissionType = v!),
                    ),
                    const SizedBox(height: 10),
                    TextField(
                      controller: _commissionMontantCtrl,
                      decoration: InputDecoration(labelText: _commissionType == 'Pourcentage' ? 'POURCENTAGE (%)' : 'MONTANT (€)'),
                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    ),
                  ]),

                  const SizedBox(height: 20),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      icon: const Icon(Icons.save, size: 18),
                      label: Text(_saving ? 'Enregistrement...' : 'Enregistrer'),
                      onPressed: _saving ? null : _save,
                    ),
                  ),
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      icon: const Icon(Icons.logout, size: 18),
                      label: const Text('Se déconnecter'),
                      style: ElevatedButton.styleFrom(backgroundColor: AppTheme.danger),
                      onPressed: () async {
                        await context.read<ApiService>().logout();
                        if (context.mounted) {
                          Navigator.of(context).pushAndRemoveUntil(
                            MaterialPageRoute(builder: (_) => const AuthGate()),
                            (route) => false,
                          );
                        }
                      },
                    ),
                  ),
                  const SizedBox(height: 24),
                ],
              ),
            ),
    );
  }

  Widget _section(String title, List<Widget> children) => Card(
        margin: const EdgeInsets.only(bottom: 16),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
              const SizedBox(height: 12),
              ...children,
            ],
          ),
        ),
      );

  Widget _timeButton(String label, TimeOfDay time, void Function(TimeOfDay) onSet) =>
      OutlinedButton(
        onPressed: () => _pickTime(label, time, onSet),
        child: Text('$label : ${_fmt(time)}', style: const TextStyle(fontSize: 13)),
      );

  Widget _alert(String msg, Color color) => Container(
        width: double.infinity, padding: const EdgeInsets.all(12), margin: const EdgeInsets.only(bottom: 12),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(8)),
        child: Text(msg, style: TextStyle(color: color, fontWeight: FontWeight.w600, fontSize: 13)),
      );
}
