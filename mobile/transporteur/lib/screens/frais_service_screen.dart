import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class FraisServiceScreen extends StatefulWidget {
  const FraisServiceScreen({super.key});

  @override
  State<FraisServiceScreen> createState() => _FraisServiceScreenState();
}

class _FraisServiceScreenState extends State<FraisServiceScreen> {
  bool _loading = true;
  bool _saving = false;
  bool _utiliseDefaut = true;
  String _type = 'Fixe';
  final _valeurCtrl = TextEditingController();
  String _defautLabel = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final api = context.read<ApiService>();
    final res = await api.getFraisService();
    if (!mounted) return;
    if (!res.containsKey('error')) {
      _utiliseDefaut = res['utiliseDefaut'] == true;
      _type = res['type']?.toString() ?? 'Fixe';
      _valeurCtrl.text = res['valeur']?.toString() ?? '';
      final dType = res['defautType']?.toString() ?? 'Fixe';
      final dVal = res['defautValeur']?.toString() ?? '0';
      _defautLabel = dType == 'Pourcentage' ? '$dVal % du transport' : '$dVal € fixes';
    }
    setState(() => _loading = false);
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    final api = context.read<ApiService>();
    final res = await api.setFraisService(
      utiliseDefaut: _utiliseDefaut,
      type: _utiliseDefaut ? null : _type,
      valeur: _utiliseDefaut ? null : double.tryParse(_valeurCtrl.text),
    );
    if (!mounted) return;
    setState(() => _saving = false);
    if (res.containsKey('error')) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['error'])));
    } else {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Frais de service enregistrés.')));
      Navigator.pop(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Frais de service')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(20),
              children: [
                Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: AppTheme.primary.withValues(alpha: 0.06),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text('Frais par défaut de la plateforme : $_defautLabel',
                      style: const TextStyle(fontSize: 13)),
                ),
                const SizedBox(height: 16),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  value: _utiliseDefaut,
                  onChanged: (v) => setState(() => _utiliseDefaut = v),
                  title: const Text('Utiliser les frais par défaut', style: TextStyle(fontWeight: FontWeight.w700)),
                  subtitle: const Text('Décochez pour définir vos propres frais de service'),
                ),
                if (!_utiliseDefaut) ...[
                  const SizedBox(height: 12),
                  const Text('Type de frais', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppTheme.textMuted)),
                  const SizedBox(height: 6),
                  SegmentedButton<String>(
                    segments: const [
                      ButtonSegment(value: 'Fixe', label: Text('Montant fixe (€)')),
                      ButtonSegment(value: 'Pourcentage', label: Text('Pourcentage (%)')),
                    ],
                    selected: {_type},
                    onSelectionChanged: (s) => setState(() => _type = s.first),
                  ),
                  const SizedBox(height: 16),
                  TextField(
                    controller: _valeurCtrl,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: InputDecoration(
                      labelText: _type == 'Pourcentage' ? 'Pourcentage (%)' : 'Montant (€)',
                      hintText: _type == 'Pourcentage' ? '5' : '5',
                    ),
                  ),
                ],
                const SizedBox(height: 28),
                ElevatedButton(
                  onPressed: _saving ? null : _save,
                  child: _saving
                      ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                      : const Text('Enregistrer'),
                ),
              ],
            ),
    );
  }
}
