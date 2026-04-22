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
  String _commissionType = 'Fixe';
  final Set<String> _joursOuverture = {'Lun', 'Mar', 'Mer', 'Jeu', 'Ven'};

  static const _tousLesJours = ['Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam', 'Dim'];
  static const _commissionTypes = ['Fixe', 'Pourcentage'];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final api = context.read<ApiService>();
    final res = await api.getProfil();
    if (!mounted) return;

    if (res.containsKey('error')) {
      setState(() {
        _loading = false;
        _error = res['error'];
      });
      return;
    }

    setState(() {
      _profil = res;
      _adresseCtrl.text = res['adresse']?.toString() ?? '';
      _commissionMontantCtrl.text =
          res['commissionMontant']?.toString() ?? '0';
      _commissionType = res['commissionType']?.toString() ?? 'Fixe';

      final ho = res['heureOuverture']?.toString();
      if (ho != null && ho.contains(':')) {
        final parts = ho.split(':');
        _heureOuverture = TimeOfDay(
            hour: int.tryParse(parts[0]) ?? 9,
            minute: int.tryParse(parts[1]) ?? 0);
      }
      final hf = res['heureFermeture']?.toString();
      if (hf != null && hf.contains(':')) {
        final parts = hf.split(':');
        _heureFermeture = TimeOfDay(
            hour: int.tryParse(parts[0]) ?? 18,
            minute: int.tryParse(parts[1]) ?? 0);
      }

      final jours = res['joursOuverture']?.toString();
      if (jours != null && jours.isNotEmpty) {
        _joursOuverture
          ..clear()
          ..addAll(jours.split(',').map((j) => j.trim()));
      }

      _loading = false;
    });
  }

  String _formatTime(TimeOfDay t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  Future<void> _pickTime(bool isOuverture) async {
    final picked = await showTimePicker(
      context: context,
      initialTime: isOuverture ? _heureOuverture : _heureFermeture,
    );
    if (picked != null) {
      setState(() {
        if (isOuverture) {
          _heureOuverture = picked;
        } else {
          _heureFermeture = picked;
        }
      });
    }
  }

  Future<void> _save() async {
    setState(() {
      _saving = true;
      _error = null;
      _success = null;
    });

    final api = context.read<ApiService>();
    final res = await api.updateProfil({
      'adresse': _adresseCtrl.text.trim(),
      'heureOuverture': _formatTime(_heureOuverture),
      'heureFermeture': _formatTime(_heureFermeture),
      'joursOuverture': _joursOuverture.join(','),
      'commissionType': _commissionType,
      'commissionMontant':
          double.tryParse(_commissionMontantCtrl.text.trim()) ?? 0,
    });

    if (!mounted) return;
    setState(() {
      _saving = false;
      if (res.containsKey('error')) {
        _error = res['error'];
      } else {
        _success = 'Profil mis a jour avec succes.';
      }
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
                  if (_success != null) _alertWidget(_success!, AppTheme.success),
                  if (_error != null) _alertWidget(_error!, AppTheme.danger),

                  // Avatar
                  Center(
                    child: Container(
                      width: 80,
                      height: 80,
                      decoration: BoxDecoration(
                        color: AppTheme.primary,
                        borderRadius: BorderRadius.circular(40),
                      ),
                      child: const Icon(Icons.store,
                          size: 40, color: Colors.white),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Center(
                    child: Text(
                      _profil?['nomRelais']?.toString() ?? 'Point Relais',
                      style: const TextStyle(
                          fontSize: 18, fontWeight: FontWeight.w800),
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Infos
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Informations',
                              style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w800)),
                          const SizedBox(height: 12),
                          _row('Role', 'Point Relais'),
                          _row('Ville',
                              _profil?['ville']?.toString() ?? '-'),
                          _row('Pays',
                              _profil?['pays']?.toString() ?? '-'),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 16),

                  // Adresse
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Adresse',
                              style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w800)),
                          const SizedBox(height: 12),
                          TextField(
                            controller: _adresseCtrl,
                            decoration: const InputDecoration(
                                labelText: 'ADRESSE'),
                            maxLines: 2,
                          ),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 16),

                  // Horaires
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Horaires d\'ouverture',
                              style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w800)),
                          const SizedBox(height: 12),
                          Row(
                            children: [
                              Expanded(
                                child: _timePicker(
                                    'OUVERTURE', _heureOuverture, true),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: _timePicker(
                                    'FERMETURE', _heureFermeture, false),
                              ),
                            ],
                          ),
                          const SizedBox(height: 16),
                          const Text('JOURS D\'OUVERTURE',
                              style: TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w800,
                                  color: AppTheme.textMuted,
                                  letterSpacing: 1)),
                          const SizedBox(height: 8),
                          Wrap(
                            spacing: 6,
                            runSpacing: 6,
                            children: _tousLesJours
                                .map((j) => _jourChip(j))
                                .toList(),
                          ),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 16),

                  // Commission
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Commission',
                              style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w800)),
                          const SizedBox(height: 12),
                          DropdownButtonFormField<String>(
                            initialValue: _commissionType,
                            decoration: const InputDecoration(
                                labelText: 'TYPE DE COMMISSION'),
                            items: _commissionTypes
                                .map((t) => DropdownMenuItem(
                                    value: t, child: Text(t)))
                                .toList(),
                            onChanged: (v) =>
                                setState(() => _commissionType = v!),
                          ),
                          const SizedBox(height: 10),
                          TextField(
                            controller: _commissionMontantCtrl,
                            decoration: InputDecoration(
                              labelText: _commissionType == 'Pourcentage'
                                  ? 'POURCENTAGE (%)'
                                  : 'MONTANT (EUR)',
                            ),
                            keyboardType: const TextInputType.numberWithOptions(
                                decimal: true),
                          ),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 20),

                  // Save button
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      icon: const Icon(Icons.save, size: 18),
                      label: const Text('Enregistrer'),
                      onPressed: _saving ? null : _save,
                    ),
                  ),

                  const SizedBox(height: 12),

                  // Logout button
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      icon: const Icon(Icons.logout, size: 18),
                      label: const Text('Se deconnecter'),
                      style: ElevatedButton.styleFrom(
                          backgroundColor: AppTheme.danger),
                      onPressed: () async {
                        await context.read<ApiService>().logout();
                        if (context.mounted) {
                          Navigator.of(context).pushAndRemoveUntil(
                            MaterialPageRoute(
                                builder: (_) => const AuthGate()),
                            (route) => false,
                          );
                        }
                      },
                    ),
                  ),

                  const SizedBox(height: 12),

                  // App info
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('Application',
                              style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.w800)),
                          const SizedBox(height: 12),
                          _row('Version', '1.0.0'),
                          _row('Plateforme', 'Colis Express'),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _timePicker(String label, TimeOfDay time, bool isOuverture) {
    return InkWell(
      onTap: () => _pickTime(isOuverture),
      child: InputDecorator(
        decoration: InputDecoration(labelText: label),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(_formatTime(time),
                style: const TextStyle(
                    fontSize: 16, fontWeight: FontWeight.w600)),
            const Icon(Icons.access_time, size: 20, color: AppTheme.primary),
          ],
        ),
      ),
    );
  }

  Widget _jourChip(String jour) => FilterChip(
        label: Text(jour, style: const TextStyle(fontSize: 12)),
        selected: _joursOuverture.contains(jour),
        onSelected: (s) => setState(
            () => s ? _joursOuverture.add(jour) : _joursOuverture.remove(jour)),
        selectedColor: AppTheme.primary.withValues(alpha: 0.15),
        checkmarkColor: AppTheme.primary,
      );

  Widget _row(String label, String value) => Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Row(
          children: [
            SizedBox(
                width: 100,
                child: Text(label,
                    style: const TextStyle(
                        color: AppTheme.textMuted, fontSize: 13))),
            Expanded(
                child: Text(value,
                    style: const TextStyle(
                        fontWeight: FontWeight.w600, fontSize: 13))),
          ],
        ),
      );

  Widget _alertWidget(String msg, Color color) => Container(
        width: double.infinity,
        padding: const EdgeInsets.all(12),
        margin: const EdgeInsets.only(bottom: 12),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Text(msg,
            style: TextStyle(
                color: color, fontWeight: FontWeight.w600, fontSize: 13)),
      );
}
