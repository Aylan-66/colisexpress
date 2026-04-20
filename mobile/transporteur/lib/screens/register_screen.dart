import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class RegisterScreen extends StatefulWidget {
  final VoidCallback onRegister;
  const RegisterScreen({super.key, required this.onRegister});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _prenomCtrl = TextEditingController();
  final _nomCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _telCtrl = TextEditingController();
  final _pwdCtrl = TextEditingController();
  final _pwd2Ctrl = TextEditingController();
  String _vehicule = 'Fourgon 12m³';
  final Set<String> _corridors = {};
  bool _loading = false;
  String? _error;

  Future<void> _register() async {
    if (_pwdCtrl.text != _pwd2Ctrl.text) {
      setState(() => _error = 'Les mots de passe ne correspondent pas.');
      return;
    }
    setState(() { _loading = true; _error = null; });

    final api = context.read<ApiService>();
    final res = await api.register(
      prenom: _prenomCtrl.text.trim(),
      nom: _nomCtrl.text.trim(),
      email: _emailCtrl.text.trim(),
      telephone: _telCtrl.text.trim(),
      motDePasse: _pwdCtrl.text,
      typeVehicule: _vehicule,
      corridorsActifs: _corridors.join(','),
    );

    setState(() => _loading = false);
    if (res.containsKey('error')) {
      setState(() => _error = res['error']);
    } else {
      widget.onRegister();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Inscription transporteur')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
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

            const Text('IDENTITÉ', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
            const SizedBox(height: 10),
            Row(children: [
              Expanded(child: TextField(controller: _prenomCtrl, decoration: const InputDecoration(labelText: 'PRÉNOM'))),
              const SizedBox(width: 10),
              Expanded(child: TextField(controller: _nomCtrl, decoration: const InputDecoration(labelText: 'NOM'))),
            ]),
            const SizedBox(height: 10),
            TextField(controller: _emailCtrl, decoration: const InputDecoration(labelText: 'EMAIL'), keyboardType: TextInputType.emailAddress),
            const SizedBox(height: 10),
            TextField(controller: _telCtrl, decoration: const InputDecoration(labelText: 'TÉLÉPHONE'), keyboardType: TextInputType.phone),
            const SizedBox(height: 10),
            TextField(controller: _pwdCtrl, decoration: const InputDecoration(labelText: 'MOT DE PASSE'), obscureText: true),
            const SizedBox(height: 10),
            TextField(controller: _pwd2Ctrl, decoration: const InputDecoration(labelText: 'CONFIRMER MOT DE PASSE'), obscureText: true),

            const SizedBox(height: 24),
            const Text('VÉHICULE', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
            const SizedBox(height: 10),
            DropdownButtonFormField<String>(
              initialValue: _vehicule,
              decoration: const InputDecoration(labelText: 'TYPE DE VÉHICULE'),
              items: ['Utilitaire 6m³', 'Fourgon 8m³', 'Fourgon 12m³', 'Camion 20m³', 'Camion 30m³+', 'Véhicule particulier']
                  .map((v) => DropdownMenuItem(value: v, child: Text(v))).toList(),
              onChanged: (v) => _vehicule = v!,
            ),

            const SizedBox(height: 20),
            const Text('CORRIDORS', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              children: [
                _corridorChip('FR-DZ', 'France → Algérie'),
                _corridorChip('FR-MA', 'France → Maroc'),
                _corridorChip('FR-TN', 'France → Tunisie'),
              ],
            ),

            const SizedBox(height: 28),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _loading ? null : _register,
                child: _loading
                    ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Text('Créer mon compte'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _corridorChip(String code, String label) => FilterChip(
        label: Text(label, style: const TextStyle(fontSize: 12)),
        selected: _corridors.contains(code),
        onSelected: (s) => setState(() => s ? _corridors.add(code) : _corridors.remove(code)),
        selectedColor: AppTheme.primary.withValues(alpha: 0.15),
        checkmarkColor: AppTheme.primary,
      );
}
